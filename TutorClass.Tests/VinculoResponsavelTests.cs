using Microsoft.EntityFrameworkCore;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;
using TutorClass.Domain.Common;
using TutorClass.Domain.Notifications;
using TutorClass.Domain.Users;
using TutorClass.Infrastructure.Persistence;

namespace TutorClass.Tests;

public sealed class VinculoResponsavelTests
{
    [Fact]
    public async Task Responsavel_solicita_vinculo_e_administradores_sao_notificados()
    {
        await using var db = CreateDbContext();
        var cenario = Seed(db);
        var service = CreateService(db);

        var resposta = await service.SolicitarAsync(cenario.ResponsavelId, new SolicitarVinculoRequest("  Ana   Clara Souza ", "Mae"), CancellationToken.None);

        Assert.Equal(StatusSolicitacaoVinculo.Pendente, resposta.Status);
        Assert.Equal("Ana Clara Souza", resposta.NomeAlunoInformado);
        Assert.Empty(db.ResponsaveisAlunos);
        Assert.Contains(db.Notificacoes, n => n.UsuarioId == cenario.AdminId && n.Tipo == TipoNotificacao.NovaSolicitacaoVinculo);
    }

    [Fact]
    public async Task Solicitacao_exige_nome_completo_e_bloqueia_duplicada_pendente()
    {
        await using var db = CreateDbContext();
        var cenario = Seed(db);
        var service = CreateService(db);

        await Assert.ThrowsAsync<DomainException>(() =>
            service.SolicitarAsync(cenario.ResponsavelId, new SolicitarVinculoRequest("Ana", "Mae"), CancellationToken.None));

        await service.SolicitarAsync(cenario.ResponsavelId, new SolicitarVinculoRequest("Ana Clara Souza", "Mae"), CancellationToken.None);
        await Assert.ThrowsAsync<DomainException>(() =>
            service.SolicitarAsync(cenario.ResponsavelId, new SolicitarVinculoRequest("ana clara souza", "Mae"), CancellationToken.None));
    }

    [Fact]
    public async Task Solicitacao_so_e_aceita_para_aluno_ja_cadastrado_com_nome_completo()
    {
        await using var db = CreateDbContext();
        var cenario = Seed(db);
        var service = CreateService(db);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            service.SolicitarAsync(cenario.ResponsavelId, new SolicitarVinculoRequest("Maria Inexistente Silva", "Mae"), CancellationToken.None));
        Assert.Contains("Nao encontramos", ex.Message);
        // Nome parcial de aluno existente tambem nao vale: e preciso o nome completo cadastrado.
        await Assert.ThrowsAsync<DomainException>(() =>
            service.SolicitarAsync(cenario.ResponsavelId, new SolicitarVinculoRequest("Ana Clara", "Mae"), CancellationToken.None));
        Assert.Empty(db.SolicitacoesVinculoResponsavel);
        Assert.DoesNotContain(db.Notificacoes, n => n.Tipo == TipoNotificacao.NovaSolicitacaoVinculo);

        // Caixa e acentos sao ignorados.
        var ok = await service.SolicitarAsync(cenario.ResponsavelId, new SolicitarVinculoRequest("ANA CLARA SOUZA", "Mae"), CancellationToken.None);
        Assert.Equal(StatusSolicitacaoVinculo.Pendente, ok.Status);
    }

    [Fact]
    public async Task Somente_responsavel_ativo_pode_solicitar_vinculo()
    {
        await using var db = CreateDbContext();
        var cenario = Seed(db);
        var service = CreateService(db);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.SolicitarAsync(cenario.AlunoId, new SolicitarVinculoRequest("Ana Clara Souza", "Mae"), CancellationToken.None));
    }

    [Fact]
    public async Task Administrador_ve_candidatos_pelo_nome_ignorando_caixa()
    {
        await using var db = CreateDbContext();
        var cenario = Seed(db);
        var service = CreateService(db);
        await service.SolicitarAsync(cenario.ResponsavelId, new SolicitarVinculoRequest("ana clara souza", "Mae"), CancellationToken.None);

        var lista = await service.ListarAdminAsync(cenario.AdminId, StatusSolicitacaoVinculo.Pendente, CancellationToken.None);

        var item = Assert.Single(lista);
        Assert.Equal("Responsavel Teste", item.ResponsavelNome);
        Assert.Contains(item.Candidatos, c => c.Id == cenario.AlunoId);
        Assert.DoesNotContain(item.Candidatos, c => c.Id == cenario.OutroAlunoId);
    }

    [Fact]
    public async Task Aprovacao_cria_vinculo_e_responsavel_passa_a_ver_o_aluno()
    {
        await using var db = CreateDbContext();
        var cenario = Seed(db);
        var service = CreateService(db);
        var solicitacao = await service.SolicitarAsync(cenario.ResponsavelId, new SolicitarVinculoRequest("Ana Clara Souza", "Mae"), CancellationToken.None);

        await service.AprovarAsync(solicitacao.Id, cenario.AdminId, new AprovarVinculoRequest(cenario.AlunoId), CancellationToken.None);

        var vinculo = Assert.Single(db.ResponsaveisAlunos);
        Assert.Equal(cenario.ResponsavelId, vinculo.ResponsavelId);
        Assert.Equal(cenario.AlunoId, vinculo.AlunoId);
        Assert.Equal("Mae", vinculo.Parentesco);
        Assert.True(new ResponsavelAlunoService(db).PossuiVinculo(cenario.ResponsavelId, cenario.AlunoId));
        var minhas = await service.ListarMinhasAsync(cenario.ResponsavelId, CancellationToken.None);
        Assert.Equal(StatusSolicitacaoVinculo.Aprovada, Assert.Single(minhas).Status);
        Assert.Contains(db.Notificacoes, n => n.UsuarioId == cenario.ResponsavelId && n.Tipo == TipoNotificacao.VinculoResponsavelAtualizado);
        Assert.Contains(db.Notificacoes, n => n.UsuarioId == cenario.AlunoId && n.Tipo == TipoNotificacao.VinculoResponsavelAtualizado);

        await Assert.ThrowsAsync<DomainException>(() =>
            service.AprovarAsync(solicitacao.Id, cenario.AdminId, new AprovarVinculoRequest(cenario.AlunoId), CancellationToken.None));
    }

    [Fact]
    public async Task Recusa_exige_motivo_e_nao_cria_vinculo()
    {
        await using var db = CreateDbContext();
        var cenario = Seed(db);
        var service = CreateService(db);
        var solicitacao = await service.SolicitarAsync(cenario.ResponsavelId, new SolicitarVinculoRequest("Ana Clara Souza", "Mae"), CancellationToken.None);

        await Assert.ThrowsAsync<DomainException>(() =>
            service.RecusarAsync(solicitacao.Id, cenario.AdminId, new RecusarVinculoRequest(" "), CancellationToken.None));

        await service.RecusarAsync(solicitacao.Id, cenario.AdminId, new RecusarVinculoRequest("Nao foi possivel confirmar o parentesco."), CancellationToken.None);

        Assert.Empty(db.ResponsaveisAlunos);
        var minhas = await service.ListarMinhasAsync(cenario.ResponsavelId, CancellationToken.None);
        var item = Assert.Single(minhas);
        Assert.Equal(StatusSolicitacaoVinculo.Recusada, item.Status);
        Assert.Equal("Nao foi possivel confirmar o parentesco.", item.Motivo);
    }

    [Fact]
    public async Task Somente_administrador_ativo_decide_a_solicitacao()
    {
        await using var db = CreateDbContext();
        var cenario = Seed(db);
        var service = CreateService(db);
        var solicitacao = await service.SolicitarAsync(cenario.ResponsavelId, new SolicitarVinculoRequest("Ana Clara Souza", "Mae"), CancellationToken.None);

        await Assert.ThrowsAsync<DomainException>(() =>
            service.AprovarAsync(solicitacao.Id, cenario.ResponsavelId, new AprovarVinculoRequest(cenario.AlunoId), CancellationToken.None));
        await Assert.ThrowsAsync<DomainException>(() =>
            service.ListarAdminAsync(cenario.ResponsavelId, null, CancellationToken.None));
        Assert.Empty(db.ResponsaveisAlunos);
    }

    private static VinculoResponsavelService CreateService(TutorClassDbContext db) => new(db, new NotificacaoService(db));

    private static Scenario Seed(TutorClassDbContext db)
    {
        var admin = new Administrador("Admin Teste", "admin.vinculo@teste.local", "hash");
        var responsavel = new Responsavel("Responsavel Teste", "responsavel.vinculo@teste.local", "hash");
        var aluno = new Aluno("Ana Clara Sousa", "ana.vinculo@teste.local", "hash");
        var alunaAcentuada = new Aluno("Ana Clara Souza", "ana.souza.vinculo@teste.local", "hash");
        var outro = new Aluno("Bruno Lima", "bruno.vinculo@teste.local", "hash");
        db.AddRange(admin, responsavel, aluno, alunaAcentuada, outro);
        db.SaveChanges();
        return new Scenario(admin.Id, responsavel.Id, alunaAcentuada.Id, outro.Id);
    }

    private static TutorClassDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TutorClassDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TutorClassDbContext(options);
    }

    private sealed record Scenario(Guid AdminId, Guid ResponsavelId, Guid AlunoId, Guid OutroAlunoId);
}
