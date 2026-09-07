using Microsoft.EntityFrameworkCore;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;
using TutorClass.Domain.Common;
using TutorClass.Domain.Lessons;
using TutorClass.Domain.Mentoring;
using TutorClass.Domain.Plans;
using TutorClass.Domain.Subjects;
using TutorClass.Domain.Teachers;
using TutorClass.Domain.Users;
using TutorClass.Infrastructure.Persistence;

namespace TutorClass.Tests;

public sealed class PlanosMentoriaTests
{
    [Fact]
    public async Task Gratuito_nao_possui_ia_educacional()
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db);
        var service = new PlanoAlunoService(db);

        var result = await service.PodeUsarIaAsync(cenario.AlunoGratuitoId, CancellationToken.None);

        Assert.False(result.Permitido);
        Assert.Equal(PlanoAluno.Gratuito, result.PlanoAtual);
    }

    [Fact]
    public async Task Foco_permite_mensagens_ate_limite_7_e_bloqueia_a_oitava()
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db);
        var service = new PlanoAlunoService(db);
        await service.AssinarAsync(cenario.AlunoFocoId, new AssinarPlanoRequest(PlanoAluno.Foco), CancellationToken.None);

        for (var i = 0; i < 7; i++)
        {
            var uso = await service.RegistrarUsoIaAsync(cenario.AlunoFocoId, CancellationToken.None);
            Assert.True(uso.Permitido);
        }

        await Assert.ThrowsAsync<DomainException>(() => service.RegistrarUsoIaAsync(cenario.AlunoFocoId, CancellationToken.None));
    }

    [Fact]
    public async Task Mentoria_nao_possui_limite_diario_do_plano_para_ia()
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db);
        var service = new PlanoAlunoService(db);
        await service.AssinarAsync(cenario.AlunoMentoriaId, new AssinarPlanoRequest(PlanoAluno.Mentoria), CancellationToken.None);

        for (var i = 0; i < 10; i++)
        {
            var uso = await service.RegistrarUsoIaAsync(cenario.AlunoMentoriaId, CancellationToken.None);
            Assert.True(uso.Permitido);
            Assert.Null(uso.LimiteDiario);
        }
    }

    [Fact]
    public async Task Promocao_do_primeiro_mes_e_aplicada_apenas_uma_vez()
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db);
        var service = new PlanoAlunoService(db);

        var primeira = await service.AssinarAsync(cenario.AlunoFocoId, new AssinarPlanoRequest(PlanoAluno.Foco), CancellationToken.None);
        await service.CancelarAtualAsync(cenario.AlunoFocoId, CancellationToken.None);
        var segunda = await service.AssinarAsync(cenario.AlunoFocoId, new AssinarPlanoRequest(PlanoAluno.Foco), CancellationToken.None);

        Assert.Equal(19.99m, primeira.ValorCobradoPrimeiroMes);
        Assert.True(primeira.UsouPromocaoPrimeiraAssinatura);
        Assert.Equal(29.99m, segunda.ValorCobradoPrimeiroMes);
        Assert.False(segunda.UsouPromocaoPrimeiraAssinatura);
    }

    [Fact]
    public async Task Aluno_sem_mentoria_nao_pode_selecionar_mentor()
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db);
        var service = new MentoriaService(db);

        await Assert.ThrowsAsync<DomainException>(() => service.SelecionarMentorAsync(
            cenario.AlunoGratuitoId,
            new SelecionarMentorRequest(cenario.MentorHelenaId, null),
            CancellationToken.None));
    }

    [Fact]
    public async Task Aluno_mentoria_pode_selecionar_mentor_habilitado()
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db);
        await new PlanoAlunoService(db).AssinarAsync(cenario.AlunoMentoriaId, new AssinarPlanoRequest(PlanoAluno.Mentoria), CancellationToken.None);
        var service = new MentoriaService(db);

        var mentoria = await service.SelecionarMentorAsync(
            cenario.AlunoMentoriaId,
            new SelecionarMentorRequest(cenario.MentorHelenaId, null),
            CancellationToken.None);

        Assert.True(mentoria.Ativo);
        Assert.Equal(1, mentoria.Slot);
    }

    [Fact]
    public async Task Professor_sem_mentoria_habilitada_nao_pode_ser_selecionado()
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db);
        await new PlanoAlunoService(db).AssinarAsync(cenario.AlunoMentoriaId, new AssinarPlanoRequest(PlanoAluno.Mentoria), CancellationToken.None);
        var service = new MentoriaService(db);

        await Assert.ThrowsAsync<DomainException>(() => service.SelecionarMentorAsync(
            cenario.AlunoMentoriaId,
            new SelecionarMentorRequest(cenario.ProfessorSemMentoriaId, null),
            CancellationToken.None));
    }

    [Fact]
    public async Task Professor_lotado_nao_pode_receber_novo_mentorado()
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db);
        var planos = new PlanoAlunoService(db);
        await planos.AssinarAsync(cenario.AlunoMentoriaId, new AssinarPlanoRequest(PlanoAluno.Mentoria), CancellationToken.None);
        await planos.AssinarAsync(cenario.AlunoExtraId, new AssinarPlanoRequest(PlanoAluno.Mentoria), CancellationToken.None);
        var service = new MentoriaService(db);

        await service.SelecionarMentorAsync(cenario.AlunoMentoriaId, new SelecionarMentorRequest(cenario.MentorRafaelLotadoId, null), CancellationToken.None);

        await Assert.ThrowsAsync<DomainException>(() => service.SelecionarMentorAsync(
            cenario.AlunoExtraId,
            new SelecionarMentorRequest(cenario.MentorRafaelLotadoId, null),
            CancellationToken.None));
    }

    [Fact]
    public async Task Aluno_nao_pode_ter_mais_de_dois_mentores_ativos()
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db);
        await new PlanoAlunoService(db).AssinarAsync(cenario.AlunoMentoriaId, new AssinarPlanoRequest(PlanoAluno.Mentoria), CancellationToken.None);
        var service = new MentoriaService(db);

        await service.SelecionarMentorAsync(cenario.AlunoMentoriaId, new SelecionarMentorRequest(cenario.MentorHelenaId, null), CancellationToken.None);
        await service.SelecionarMentorAsync(cenario.AlunoMentoriaId, new SelecionarMentorRequest(cenario.MentorMarcosId, null), CancellationToken.None);

        await Assert.ThrowsAsync<DomainException>(() => service.SelecionarMentorAsync(
            cenario.AlunoMentoriaId,
            new SelecionarMentorRequest(cenario.MentorRafaelLotadoId, null),
            CancellationToken.None));
    }

    [Fact]
    public async Task Troca_encerra_vinculo_antigo_sem_apagar_historico()
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db);
        await new PlanoAlunoService(db).AssinarAsync(cenario.AlunoMentoriaId, new AssinarPlanoRequest(PlanoAluno.Mentoria), CancellationToken.None);
        var service = new MentoriaService(db);
        var original = await service.SelecionarMentorAsync(cenario.AlunoMentoriaId, new SelecionarMentorRequest(cenario.MentorHelenaId, 1), CancellationToken.None);
        var aula = SeedConfirmedLesson(db, cenario.AlunoMentoriaId, cenario.MentorHelenaId, cenario.MateriaId);
        var acompanhamentoId = await service.CriarAcompanhamentoPendenteParaAulaAsync(aula, CancellationToken.None);
        SetLastExchange(db, original.Id, DateTime.UtcNow.AddDays(-31));

        var novo = await service.TrocarMentorAsync(cenario.AlunoMentoriaId, 1, new TrocarMentorRequest(cenario.MentorMarcosId), CancellationToken.None);

        Assert.NotEqual(original.Id, novo.Id);
        Assert.False(db.MentoriasAlunoProfessor.Single(m => m.Id == original.Id).Ativo);
        Assert.True(db.AcompanhamentosMentoria.Any(a => a.Id == acompanhamentoId));
    }

    [Fact]
    public async Task Aula_normal_nao_cria_acompanhamento_de_mentoria()
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db);
        var aula = SeedConfirmedLesson(db, cenario.AlunoGratuitoId, cenario.MentorHelenaId, cenario.MateriaId);
        var service = new AulaService(db);

        var result = await service.ConcluirAsync(aula.Id, cenario.MentorHelenaId, CancellationToken.None);

        Assert.True(result.AulaConcluida);
        Assert.False(result.RequerAcompanhamentoMentoria);
        Assert.Empty(db.AcompanhamentosMentoria);
    }

    [Fact]
    public async Task Aula_com_mentor_ativo_cria_acompanhamento_pendente()
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db);
        await new PlanoAlunoService(db).AssinarAsync(cenario.AlunoMentoriaId, new AssinarPlanoRequest(PlanoAluno.Mentoria), CancellationToken.None);
        var mentoria = await new MentoriaService(db).SelecionarMentorAsync(cenario.AlunoMentoriaId, new SelecionarMentorRequest(cenario.MentorHelenaId, null), CancellationToken.None);
        var aula = SeedConfirmedLesson(db, cenario.AlunoMentoriaId, cenario.MentorHelenaId, cenario.MateriaId);
        var service = new AulaService(db);

        var result = await service.ConcluirAsync(aula.Id, cenario.MentorHelenaId, CancellationToken.None);

        Assert.True(result.RequerAcompanhamentoMentoria);
        Assert.NotNull(result.AcompanhamentoMentoriaId);
        Assert.Equal(mentoria.Id, db.AcompanhamentosMentoria.Single().VinculoMentoriaId);
        Assert.Equal(StatusAcompanhamentoMentoria.Pendente, db.AcompanhamentosMentoria.Single().Status);
    }

    [Fact]
    public async Task Adicional_de_20_e_gerado_ate_duas_vezes_por_mes_e_terceiro_acompanhamento_continua_salvo()
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db);
        await new PlanoAlunoService(db).AssinarAsync(cenario.AlunoMentoriaId, new AssinarPlanoRequest(PlanoAluno.Mentoria), CancellationToken.None);
        var service = new MentoriaService(db);
        await service.SelecionarMentorAsync(cenario.AlunoMentoriaId, new SelecionarMentorRequest(cenario.MentorHelenaId, null), CancellationToken.None);

        var resultados = new List<ConclusaoAcompanhamentoMentoriaResponse>();
        for (var i = 0; i < 3; i++)
        {
            var aula = SeedConfirmedLesson(db, cenario.AlunoMentoriaId, cenario.MentorHelenaId, cenario.MateriaId);
            var acompanhamentoId = await service.CriarAcompanhamentoPendenteParaAulaAsync(aula, CancellationToken.None);
            resultados.Add(await service.ConcluirAcompanhamentoAsync(
                acompanhamentoId!.Value,
                cenario.MentorHelenaId,
                new AtualizarAcompanhamentoMentoriaRequest($"Resumo {i}", null, null, null, null, null),
                CancellationToken.None));
        }

        Assert.True(resultados[0].AdicionalGerado);
        Assert.Equal(20m, resultados[0].ValorAdicional);
        Assert.True(resultados[1].AdicionalGerado);
        Assert.False(resultados[2].AdicionalGerado);
        Assert.Equal(2, db.AdicionaisMentoria.Count());
        Assert.Equal(3, db.AcompanhamentosMentoria.Count(a => a.Status == StatusAcompanhamentoMentoria.Concluido));
    }

    private static Scenario SeedScenario(TutorClassDbContext db)
    {
        var materia = new Materia("Biologia");
        var gratuito = new Aluno("Ana Gratuita", "ana.gratis@teste.local", "hash");
        var foco = new Aluno("Bruno Foco", "bruno.foco@teste.local", "hash");
        var mentoria = new Aluno("Camila Mentoria", "camila.mentoria@teste.local", "hash");
        var extra = new Aluno("Diego Extra", "diego.extra@teste.local", "hash");
        var helena = CreateProfessor("Helena", "helena.mentor@teste.local", ofereceMentoria: true, limite: 4);
        var marcos = CreateProfessor("Marcos", "marcos.mentor@teste.local", ofereceMentoria: true, limite: 4);
        var rafael = CreateProfessor("Rafael", "rafael.mentor@teste.local", ofereceMentoria: true, limite: 1);
        var semMentoria = CreateProfessor("Patricia", "patricia.sem.mentoria@teste.local", ofereceMentoria: false, limite: 0);

        db.AddRange(materia, gratuito, foco, mentoria, extra, helena, marcos, rafael, semMentoria);
        db.SaveChanges();
        db.AddRange(
            new ProfessorMateria(helena.Id, materia.Id),
            new ProfessorMateria(marcos.Id, materia.Id),
            new ProfessorMateria(rafael.Id, materia.Id),
            new ProfessorMateria(semMentoria.Id, materia.Id));
        db.SaveChanges();

        return new Scenario(gratuito.Id, foco.Id, mentoria.Id, extra.Id, helena.Id, marcos.Id, rafael.Id, semMentoria.Id, materia.Id);
    }

    private static Professor CreateProfessor(string nome, string email, bool ofereceMentoria, int limite)
    {
        var professor = new Professor(nome, email, "hash");
        professor.AtualizarPerfilProfissional("Professor para teste de planos e mentoria.", 80m);
        professor.Aprovar();
        professor.ConfigurarMentoria(ofereceMentoria, limite);
        return professor;
    }

    private static Aula SeedConfirmedLesson(TutorClassDbContext db, Guid alunoId, Guid professorId, Guid materiaId)
    {
        var aula = new Aula(alunoId, professorId, materiaId, DateTime.UtcNow.AddDays(1).AddMinutes(db.Aulas.Count() * 90), 60, 80m);
        db.Add(aula);
        db.SaveChanges();
        aula.Confirmar(professorId);
        db.SaveChanges();
        return aula;
    }

    private static void SetLastExchange(TutorClassDbContext db, Guid mentoriaId, DateTime value)
    {
        var mentoria = db.MentoriasAlunoProfessor.Single(m => m.Id == mentoriaId);
        db.Entry(mentoria).Property(nameof(MentoriaAlunoProfessor.DataUltimaTrocaUtc)).CurrentValue = value;
        db.SaveChanges();
    }

    private static TutorClassDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TutorClassDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TutorClassDbContext(options);
    }

    private sealed record Scenario(
        Guid AlunoGratuitoId,
        Guid AlunoFocoId,
        Guid AlunoMentoriaId,
        Guid AlunoExtraId,
        Guid MentorHelenaId,
        Guid MentorMarcosId,
        Guid MentorRafaelLotadoId,
        Guid ProfessorSemMentoriaId,
        Guid MateriaId);
}
