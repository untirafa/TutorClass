using System.Reflection;
using Microsoft.EntityFrameworkCore;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;
using TutorClass.Domain.Common;
using TutorClass.Domain.Financial;
using TutorClass.Domain.Lessons;
using TutorClass.Domain.Mentoring;
using TutorClass.Domain.Plans;
using TutorClass.Domain.Subjects;
using TutorClass.Domain.Teachers;
using TutorClass.Domain.Users;
using TutorClass.Infrastructure.Persistence;

namespace TutorClass.Tests;

public sealed class FinanceiroTests
{
    [Fact]
    public void Aula_de_100_gera_comissao_12_e_liquido_88()
    {
        var pagamento = new PagamentoAula(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m);

        Assert.Equal(12m, pagamento.ValorComissao);
        Assert.Equal(88m, pagamento.ValorLiquidoProfessor);
        Assert.Equal(12m, pagamento.PercentualComissao);
    }

    [Fact]
    public void Valores_com_casas_decimais_sao_calculados_com_decimal()
    {
        var pagamento = new PagamentoAula(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 99.99m);
        var monetaryProperties = typeof(PagamentoAula)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.Name.StartsWith("Valor", StringComparison.Ordinal) || p.Name == nameof(PagamentoAula.PercentualComissao))
            .ToArray();

        Assert.Equal(12.00m, pagamento.ValorComissao);
        Assert.Equal(87.99m, pagamento.ValorLiquidoProfessor);
        Assert.All(monetaryProperties, property => Assert.Equal(typeof(decimal), property.PropertyType));
    }

    [Fact]
    public async Task Aluno_pode_pagar_a_propria_aula()
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db);
        var service = new FinanceiroService(db);

        var pagamento = await service.PagarAulaAsync(cenario.AulaId, cenario.AlunoId, CancellationToken.None);

        Assert.Equal(cenario.AlunoId, pagamento.PagadorUsuarioId);
        Assert.Equal(100m, pagamento.ValorBruto);
        Assert.Equal(12m, pagamento.ValorComissao);
        Assert.Equal(88m, pagamento.ValorLiquidoProfessor);
        Assert.Equal(FormaPagamento.Pix, pagamento.FormaPagamento);
        Assert.Equal(StatusPagamentoFinanceiro.Pago, pagamento.Status);
    }

    [Theory]
    [InlineData(FormaPagamento.Pix)]
    [InlineData(FormaPagamento.Cartao)]
    public async Task Pix_e_cartao_sao_aceitos_no_pagamento_de_aula(FormaPagamento formaPagamento)
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db);
        var service = new FinanceiroService(db);

        var pagamento = await service.PagarAulaAsync(cenario.AulaId, cenario.AlunoId, formaPagamento, CancellationToken.None);

        Assert.Equal(formaPagamento, pagamento.FormaPagamento);
    }

    [Fact]
    public async Task Aula_nao_recebe_dois_pagamentos_validos_mesmo_com_pagadores_diferentes()
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db);
        var service = new FinanceiroService(db);

        await service.PagarAulaAsync(cenario.AulaId, cenario.AlunoId, FormaPagamento.Pix, CancellationToken.None);

        await Assert.ThrowsAsync<DomainException>(() => service.PagarAulaAsync(cenario.AulaId, cenario.ResponsavelId, FormaPagamento.Cartao, CancellationToken.None));
        Assert.Single(db.PagamentosAula);
    }

    [Fact]
    public async Task Aula_cancelada_nao_pode_receber_pagamento()
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db);
        var aulaService = new AulaService(db);
        var financeiro = new FinanceiroService(db);

        await aulaService.CancelarAsync(cenario.AulaId, cenario.AlunoId, null, CancellationToken.None);

        await Assert.ThrowsAsync<DomainException>(() => financeiro.PagarAulaAsync(cenario.AulaId, cenario.AlunoId, FormaPagamento.Pix, CancellationToken.None));
    }

    [Fact]
    public async Task Responsavel_vinculado_pode_pagar_aula_do_aluno()
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db);
        var service = new FinanceiroService(db);

        var pagamento = await service.PagarAulaAsync(cenario.AulaId, cenario.ResponsavelId, CancellationToken.None);

        Assert.Equal(cenario.AlunoId, pagamento.AlunoId);
        Assert.Equal(cenario.ResponsavelId, pagamento.PagadorUsuarioId);
    }

    [Fact]
    public async Task Responsavel_nao_vinculado_nao_paga_nem_consulta_financeiro_do_aluno()
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db);
        var service = new FinanceiroService(db);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.PagarAulaAsync(cenario.AulaId, cenario.ResponsavelNaoVinculadoId, CancellationToken.None));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.HistoricoAlunoAsync(cenario.ResponsavelNaoVinculadoId, cenario.AlunoId, CancellationToken.None));
    }

    [Fact]
    public async Task Aluno_pode_ser_beneficiario_da_assinatura_paga_por_responsavel()
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db);
        var service = new PlanoAlunoService(db);

        var assinatura = await service.AssinarParaBeneficiarioAsync(
            cenario.ResponsavelId,
            cenario.AlunoId,
            new AssinarPlanoRequest(PlanoAluno.Mentoria, cenario.AlunoId),
            CancellationToken.None);

        Assert.Equal(cenario.AlunoId, assinatura.AlunoId);
        Assert.Equal(cenario.ResponsavelId, assinatura.PagadorUsuarioId);
        Assert.Equal(59.99m, assinatura.ValorCobradoPrimeiroMes);
        Assert.Equal(PlanoAluno.Mentoria, service.ObterPlanoAtual(cenario.AlunoId));
    }

    [Fact]
    public async Task Renovacao_ou_nova_assinatura_apos_promocao_usa_preco_normal()
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db);
        var service = new PlanoAlunoService(db);

        var primeira = await service.AssinarParaBeneficiarioAsync(cenario.ResponsavelId, cenario.AlunoId, new AssinarPlanoRequest(PlanoAluno.Foco, cenario.AlunoId), CancellationToken.None);
        await service.CancelarAtualAsync(cenario.AlunoId, CancellationToken.None);
        var segunda = await service.AssinarParaBeneficiarioAsync(cenario.ResponsavelId, cenario.AlunoId, new AssinarPlanoRequest(PlanoAluno.Foco, cenario.AlunoId), CancellationToken.None);

        Assert.Equal(19.99m, primeira.ValorCobradoPrimeiroMes);
        Assert.Equal(29.99m, segunda.ValorCobradoPrimeiroMes);
    }

    [Fact]
    public async Task Cobranca_mensal_de_assinatura_respeita_promocao_e_preco_normal()
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db);
        var planos = new PlanoAlunoService(db);
        var financeiro = new FinanceiroService(db, planos);

        var assinatura = await planos.AssinarParaBeneficiarioAsync(cenario.ResponsavelId, cenario.AlunoId, new AssinarPlanoRequest(PlanoAluno.Mentoria, cenario.AlunoId), CancellationToken.None);
        var primeira = await financeiro.GerarCobrancaAssinaturaAsync(
            assinatura.Id,
            cenario.ResponsavelId,
            new GerarCobrancaAssinaturaRequest(DateTime.UtcNow.Year, DateTime.UtcNow.Month, FormaPagamento.Pix),
            CancellationToken.None);
        var proximoMes = DateTime.UtcNow.AddMonths(1);
        var segunda = await financeiro.GerarCobrancaAssinaturaAsync(
            assinatura.Id,
            cenario.ResponsavelId,
            new GerarCobrancaAssinaturaRequest(proximoMes.Year, proximoMes.Month, FormaPagamento.Cartao),
            CancellationToken.None);

        Assert.Equal(59.99m, primeira.Valor);
        Assert.Equal(FormaPagamento.Pix, primeira.FormaPagamento);
        Assert.Equal(79.99m, segunda.Valor);
        Assert.Equal(FormaPagamento.Cartao, segunda.FormaPagamento);
    }

    [Fact]
    public async Task Cobranca_duplicada_da_mesma_competencia_e_rejeitada()
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db);
        var planos = new PlanoAlunoService(db);
        var financeiro = new FinanceiroService(db, planos);
        var assinatura = await planos.AssinarAsync(cenario.AlunoId, new AssinarPlanoRequest(PlanoAluno.Foco), CancellationToken.None);
        var request = new GerarCobrancaAssinaturaRequest(DateTime.UtcNow.Year, DateTime.UtcNow.Month, FormaPagamento.Pix);

        await financeiro.GerarCobrancaAssinaturaAsync(assinatura.Id, cenario.AlunoId, request, CancellationToken.None);

        await Assert.ThrowsAsync<DomainException>(() => financeiro.GerarCobrancaAssinaturaAsync(assinatura.Id, cenario.AlunoId, request, CancellationToken.None));
    }

    [Fact]
    public async Task Gratuito_nao_gera_cobranca_de_assinatura()
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db);
        var planos = new PlanoAlunoService(db);

        await Assert.ThrowsAsync<DomainException>(() => planos.AssinarAsync(cenario.AlunoId, new AssinarPlanoRequest(PlanoAluno.Gratuito), CancellationToken.None));
    }

    [Fact]
    public async Task Assinatura_aula_e_adicional_ficam_separados_no_financeiro()
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db);
        var planos = new PlanoAlunoService(db);
        var financeiro = new FinanceiroService(db);
        var mentoria = new MentoriaService(db, planos);

        await planos.AssinarParaBeneficiarioAsync(cenario.ResponsavelId, cenario.AlunoId, new AssinarPlanoRequest(PlanoAluno.Mentoria, cenario.AlunoId), CancellationToken.None);
        await financeiro.PagarAulaAsync(cenario.AulaId, cenario.ResponsavelId, CancellationToken.None);
        await mentoria.SelecionarMentorAsync(cenario.AlunoId, new SelecionarMentorRequest(cenario.ProfessorId, null), CancellationToken.None);
        var aulaService = new AulaService(db, mentoria);
        var aulaConcluida = await aulaService.ConcluirAsync(cenario.AulaId, cenario.ProfessorId, CancellationToken.None);
        await mentoria.ConcluirAcompanhamentoAsync(
            aulaConcluida.AcompanhamentoMentoriaId!.Value,
            cenario.ProfessorId,
            new AtualizarAcompanhamentoMentoriaRequest("Resumo", null, null, null, null, null),
            CancellationToken.None);

        var historicoAluno = await financeiro.HistoricoAlunoAsync(cenario.ResponsavelId, cenario.AlunoId, CancellationToken.None);
        var resumoProfessor = await financeiro.ResumoProfessorAsync(cenario.ProfessorId, DateTime.UtcNow.Year, DateTime.UtcNow.Month, CancellationToken.None);

        Assert.Contains(historicoAluno, item => item.Tipo == "Assinatura" && item.Valor == 59.99m);
        Assert.Contains(historicoAluno, item => item.Tipo == "Aula" && item.Valor == 100m);
        Assert.DoesNotContain(historicoAluno, item => item.Tipo == "AdicionalMentoria");
        Assert.Contains(resumoProfessor.Itens, item => item.Tipo == "Aula" && item.ValorLiquidoProfessor == 88m);
        Assert.Contains(resumoProfessor.Itens, item => item.Tipo == "AdicionalMentoria" && item.ValorAdicionalMentoria == 20m);
        Assert.Equal(108m, resumoProfessor.TotalPrevisto);
    }

    [Fact]
    public async Task Acompanhamento_pendente_nao_gera_adicional_e_concluido_gera_ate_duas_vezes_no_mes()
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db);
        var planos = new PlanoAlunoService(db);
        var mentoria = new MentoriaService(db, planos);
        await planos.AssinarAsync(cenario.AlunoId, new AssinarPlanoRequest(PlanoAluno.Mentoria), CancellationToken.None);
        await mentoria.SelecionarMentorAsync(cenario.AlunoId, new SelecionarMentorRequest(cenario.ProfessorId, null), CancellationToken.None);

        var pendingAula = SeedConfirmedLesson(db, cenario.AlunoId, cenario.ProfessorId, cenario.MateriaId, 100m);
        var pendingId = await mentoria.CriarAcompanhamentoPendenteParaAulaAsync(pendingAula, CancellationToken.None);
        Assert.NotNull(pendingId);
        Assert.Empty(db.AdicionaisMentoria);

        for (var i = 0; i < 3; i++)
        {
            var aula = i == 0 ? pendingAula : SeedConfirmedLesson(db, cenario.AlunoId, cenario.ProfessorId, cenario.MateriaId, 100m);
            var acompanhamentoId = i == 0 ? pendingId!.Value : (await mentoria.CriarAcompanhamentoPendenteParaAulaAsync(aula, CancellationToken.None))!.Value;
            await mentoria.ConcluirAcompanhamentoAsync(
                acompanhamentoId,
                cenario.ProfessorId,
                new AtualizarAcompanhamentoMentoriaRequest($"Resumo {i}", null, null, null, null, null),
                CancellationToken.None);
        }

        Assert.Equal(2, db.AdicionaisMentoria.Count());
        Assert.Equal(3, db.AcompanhamentosMentoria.Count(a => a.Status == StatusAcompanhamentoMentoria.Concluido));
    }

    private static ScenarioFinanceiro SeedScenario(TutorClassDbContext db)
    {
        var aluno = new Aluno("Aluno Financeiro", "aluno.financeiro@teste.local", "hash");
        var responsavel = new Responsavel("Responsavel Financeiro", "responsavel.financeiro@teste.local", "11999990000");
        var responsavelNaoVinculado = new Responsavel("Responsavel Sem Vinculo", "responsavel.sem.vinculo@teste.local", "11888880000");
        var professor = new Professor("Professor Financeiro", "prof.financeiro@teste.local", "hash");
        professor.AtualizarPerfilProfissional("Professor para teste financeiro.", 100m);
        professor.Aprovar();
        professor.ConfigurarMentoria(true, 4);
        var materia = new Materia("Financeiro");

        db.AddRange(aluno, responsavel, responsavelNaoVinculado, professor, materia);
        db.SaveChanges();
        db.AddRange(new ResponsavelAluno(responsavel.Id, aluno.Id, "Mae"), new ProfessorMateria(professor.Id, materia.Id));
        db.SaveChanges();
        var aula = SeedConfirmedLesson(db, aluno.Id, professor.Id, materia.Id, 100m);

        return new ScenarioFinanceiro(aluno.Id, responsavel.Id, responsavelNaoVinculado.Id, professor.Id, materia.Id, aula.Id);
    }

    private static Aula SeedConfirmedLesson(TutorClassDbContext db, Guid alunoId, Guid professorId, Guid materiaId, decimal preco)
    {
        var aula = new Aula(alunoId, professorId, materiaId, DateTime.UtcNow.AddDays(1).AddMinutes(db.Aulas.Count() * 90), 60, preco);
        db.Add(aula);
        db.SaveChanges();
        aula.Confirmar(professorId);
        db.SaveChanges();
        return aula;
    }

    private static TutorClassDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TutorClassDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TutorClassDbContext(options);
    }

    private sealed record ScenarioFinanceiro(Guid AlunoId, Guid ResponsavelId, Guid ResponsavelNaoVinculadoId, Guid ProfessorId, Guid MateriaId, Guid AulaId);
}
