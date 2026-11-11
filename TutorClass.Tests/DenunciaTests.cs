using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorClass.Application.Abstractions;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;
using TutorClass.Domain.Common;
using TutorClass.Domain.Mentoring;
using TutorClass.Domain.Moderation;
using TutorClass.Domain.Notifications;
using TutorClass.Domain.Teachers;
using TutorClass.Domain.Users;
using TutorClass.Infrastructure.Persistence;

namespace TutorClass.Tests;

public sealed class DenunciaTests
{
    [Fact]
    public async Task Cria_denuncia_de_professor_e_avisa_administradores()
    {
        await using var db = CreateDbContext();
        var aluno = Add(db, new Aluno("Ana", NewEmail(), "hash"));
        var professor = Add(db, new Professor("Prof Bia", NewEmail(), "hash"));
        var admin = Add(db, new Administrador("Admin", NewEmail(), "hash"));
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var resposta = await service.CriarAsync(aluno.Id, new CriarDenunciaRequest(TipoAlvoDenuncia.Professor, professor.Id.ToString(), MotivoDenuncia.PerfilFalsoOuGolpe, "Foto de outra pessoa."), CancellationToken.None);

        Assert.Equal(StatusDenuncia.Aberta, resposta.Status);
        var denuncia = Assert.Single(db.Denuncias);
        Assert.Equal(aluno.Id, denuncia.DenuncianteId);
        Assert.Equal(professor.Id, denuncia.AlvoUsuarioId);
        Assert.Contains("Prof Bia", denuncia.ResumoAlvo);
        Assert.Single(db.Notificacoes.Where(n => n.UsuarioId == admin.Id && n.Tipo == TipoNotificacao.NovaDenuncia));
    }

    [Fact]
    public async Task Nao_permite_denunciar_a_si_mesmo()
    {
        await using var db = CreateDbContext();
        var professor = Add(db, new Professor("Prof Bia", NewEmail(), "hash"));
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<DomainException>(() => CreateService(db).CriarAsync(professor.Id,
            new CriarDenunciaRequest(TipoAlvoDenuncia.Professor, professor.Id.ToString(), MotivoDenuncia.ConteudoOfensivo, null), CancellationToken.None));
    }

    [Fact]
    public async Task Recusa_denuncia_repetida_enquanto_a_anterior_esta_em_analise()
    {
        await using var db = CreateDbContext();
        var aluno = Add(db, new Aluno("Ana", NewEmail(), "hash"));
        var professor = Add(db, new Professor("Prof Bia", NewEmail(), "hash"));
        await db.SaveChangesAsync();
        var service = CreateService(db);
        var request = new CriarDenunciaRequest(TipoAlvoDenuncia.Professor, professor.Id.ToString(), MotivoDenuncia.ConteudoOfensivo, null);

        await service.CriarAsync(aluno.Id, request, CancellationToken.None);

        await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(aluno.Id, request, CancellationToken.None));
    }

    [Fact]
    public async Task Motivo_outro_exige_descricao_com_dez_caracteres()
    {
        await using var db = CreateDbContext();
        var aluno = Add(db, new Aluno("Ana", NewEmail(), "hash"));
        var professor = Add(db, new Professor("Prof Bia", NewEmail(), "hash"));
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(aluno.Id,
            new CriarDenunciaRequest(TipoAlvoDenuncia.Professor, professor.Id.ToString(), MotivoDenuncia.Outro, "curto"), CancellationToken.None));
        var ok = await service.CriarAsync(aluno.Id,
            new CriarDenunciaRequest(TipoAlvoDenuncia.Professor, professor.Id.ToString(), MotivoDenuncia.Outro, "Descrevo o ocorrido em detalhes."), CancellationToken.None);
        Assert.Equal(StatusDenuncia.Aberta, ok.Status);
    }

    [Fact]
    public async Task Denuncia_de_avaliacao_aponta_para_o_autor_e_nao_aceita_a_propria_avaliacao()
    {
        await using var db = CreateDbContext();
        var autor = Add(db, new Aluno("Autor", NewEmail(), "hash"));
        var professor = Add(db, new Professor("Prof Bia", NewEmail(), "hash"));
        var avaliacao = Add(db, new AvaliacaoProfessor(Guid.NewGuid(), autor.Id, professor.Id, 1, "Comentario ofensivo."));
        await db.SaveChangesAsync();
        var service = CreateService(db);
        var request = new CriarDenunciaRequest(TipoAlvoDenuncia.Avaliacao, avaliacao.Id.ToString(), MotivoDenuncia.ConteudoOfensivo, null);

        await service.CriarAsync(professor.Id, request, CancellationToken.None);

        var denuncia = Assert.Single(db.Denuncias);
        Assert.Equal(autor.Id, denuncia.AlvoUsuarioId);
        Assert.Contains("Comentario ofensivo.", denuncia.ResumoAlvo);
        await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(autor.Id, request, CancellationToken.None));
    }

    [Fact]
    public async Task Denuncia_de_mensagem_exige_participante_e_guarda_o_trecho()
    {
        await using var db = CreateDbContext();
        var aluno = Add(db, new Aluno("Ana", NewEmail(), "hash"));
        var professor = Add(db, new Professor("Prof Bia", NewEmail(), "hash"));
        var intruso = Add(db, new Aluno("Intruso", NewEmail(), "hash"));
        var vinculo = Add(db, new MentoriaAlunoProfessor(aluno.Id, professor.Id, 1));
        await db.SaveChangesAsync();
        var mensagem = new MensagemMentoriaResponse("msg-1", "chat-1", vinculo.Id, professor.Id, professor.Nome, "Texto da mensagem denunciada", DateTime.UtcNow, false, null);
        var service = CreateService(db, new FakeChatStore(mensagem));
        var request = new CriarDenunciaRequest(TipoAlvoDenuncia.Mensagem, "msg-1", MotivoDenuncia.AssedioOuAmeaca, null, vinculo.Id);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CriarAsync(intruso.Id, request, CancellationToken.None));
        await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(professor.Id, request, CancellationToken.None)); // propria mensagem
        await service.CriarAsync(aluno.Id, request, CancellationToken.None);

        var denuncia = Assert.Single(db.Denuncias);
        Assert.Equal(professor.Id, denuncia.AlvoUsuarioId);
        Assert.Equal(vinculo.Id, denuncia.VinculoMentoriaId);
        Assert.Contains("Texto da mensagem denunciada", denuncia.ResumoAlvo);
    }

    [Fact]
    public async Task Limita_denuncias_por_dia()
    {
        await using var db = CreateDbContext();
        var aluno = Add(db, new Aluno("Ana", NewEmail(), "hash"));
        var professores = Enumerable.Range(0, 11).Select(i => Add(db, new Professor($"Prof {i}", NewEmail(), "hash"))).ToArray();
        await db.SaveChangesAsync();
        var service = CreateService(db);

        foreach (var professor in professores.Take(10))
        {
            await service.CriarAsync(aluno.Id, new CriarDenunciaRequest(TipoAlvoDenuncia.Professor, professor.Id.ToString(), MotivoDenuncia.ConteudoOfensivo, null), CancellationToken.None);
        }

        await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(aluno.Id,
            new CriarDenunciaRequest(TipoAlvoDenuncia.Professor, professores[10].Id.ToString(), MotivoDenuncia.ConteudoOfensivo, null), CancellationToken.None));
    }

    [Fact]
    public async Task Decisao_exige_justificativa_e_improcedente_nao_tem_sancao()
    {
        var cenario = await SeedDenunciaAsync();
        var service = CreateService(cenario.Db);

        await Assert.ThrowsAsync<DomainException>(() => service.DecidirAsync(cenario.DenunciaId, cenario.AdminId, new DecidirDenunciaRequest(true, SancaoDenuncia.Advertencia, "  "), CancellationToken.None));
        await Assert.ThrowsAsync<DomainException>(() => service.DecidirAsync(cenario.DenunciaId, cenario.AdminId, new DecidirDenunciaRequest(false, SancaoDenuncia.Suspensao, "Sem violacao."), CancellationToken.None));

        var resposta = await service.DecidirAsync(cenario.DenunciaId, cenario.AdminId, new DecidirDenunciaRequest(false, SancaoDenuncia.Nenhuma, "Sem violacao."), CancellationToken.None);

        Assert.Equal(StatusDenuncia.Improcedente, resposta.Status);
        Assert.Equal(SancaoDenuncia.Nenhuma, resposta.Sancao);
        Assert.True(cenario.Db.Usuarios.Single(u => u.Id == cenario.AlvoId).Ativo);
        Assert.Single(cenario.Db.Notificacoes.Where(n => n.UsuarioId == cenario.DenuncianteId && n.Tipo == TipoNotificacao.DenunciaAtualizada));
        await Assert.ThrowsAsync<DomainException>(() => service.DecidirAsync(cenario.DenunciaId, cenario.AdminId, new DecidirDenunciaRequest(true, SancaoDenuncia.Advertencia, "De novo."), CancellationToken.None));
    }

    [Fact]
    public async Task Advertencia_avisa_o_denunciado_sem_suspender()
    {
        var cenario = await SeedDenunciaAsync();
        var service = CreateService(cenario.Db);

        var resposta = await service.DecidirAsync(cenario.DenunciaId, cenario.AdminId, new DecidirDenunciaRequest(true, SancaoDenuncia.Advertencia, "Linguagem inadequada."), CancellationToken.None);

        Assert.Equal(StatusDenuncia.Procedente, resposta.Status);
        Assert.True(cenario.Db.Usuarios.Single(u => u.Id == cenario.AlvoId).Ativo);
        Assert.Single(cenario.Db.Notificacoes.Where(n => n.UsuarioId == cenario.AlvoId && n.Tipo == TipoNotificacao.AdvertenciaModeracao));
    }

    [Fact]
    public async Task Suspensao_desativa_a_conta_e_a_reativacao_devolve_o_acesso()
    {
        var cenario = await SeedDenunciaAsync();
        var service = CreateService(cenario.Db);

        var decidida = await service.DecidirAsync(cenario.DenunciaId, cenario.AdminId, new DecidirDenunciaRequest(true, SancaoDenuncia.Suspensao, "Ameaca a outro usuario."), CancellationToken.None);

        Assert.Equal(SancaoDenuncia.Suspensao, decidida.Sancao);
        Assert.False(decidida.AlvoAtivo);
        Assert.False(cenario.Db.Usuarios.Single(u => u.Id == cenario.AlvoId).Ativo);

        var reativada = await service.ReativarContaAsync(cenario.DenunciaId, cenario.AdminId, CancellationToken.None);

        Assert.True(reativada.AlvoAtivo);
        Assert.NotNull(reativada.ContaReativadaEmUtc);
        Assert.True(cenario.Db.Usuarios.Single(u => u.Id == cenario.AlvoId).Ativo);
        await Assert.ThrowsAsync<DomainException>(() => service.ReativarContaAsync(cenario.DenunciaId, cenario.AdminId, CancellationToken.None));
    }

    [Fact]
    public async Task Nao_aplica_sancao_a_conta_administradora()
    {
        await using var db = CreateDbContext();
        var aluno = Add(db, new Aluno("Ana", NewEmail(), "hash"));
        var admin = Add(db, new Administrador("Admin", NewEmail(), "hash"));
        var outroAdmin = Add(db, new Administrador("Outro admin", NewEmail(), "hash"));
        var denuncia = Add(db, new Denuncia(aluno.Id, outroAdmin.Id, TipoAlvoDenuncia.Mensagem, "x", MotivoDenuncia.ConteudoOfensivo, null, "Mensagem"));
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<DomainException>(() => CreateService(db).DecidirAsync(denuncia.Id, admin.Id,
            new DecidirDenunciaRequest(true, SancaoDenuncia.Suspensao, "Tentativa."), CancellationToken.None));
        Assert.True(db.Usuarios.Single(u => u.Id == outroAdmin.Id).Ativo);
    }

    [Fact]
    public async Task Lista_minhas_denuncias_somente_do_proprio_usuario_e_resume_a_fila()
    {
        var cenario = await SeedDenunciaAsync();
        var service = CreateService(cenario.Db);

        var minhas = await service.ListarMinhasAsync(cenario.DenuncianteId, CancellationToken.None);
        var deOutro = await service.ListarMinhasAsync(cenario.AlvoId, CancellationToken.None);
        var resumo = await service.ObterResumoAsync(CancellationToken.None);
        var fila = await service.ListarAdminAsync(StatusDenuncia.Aberta, 1, 30, CancellationToken.None);

        Assert.Single(minhas);
        Assert.Empty(deOutro);
        Assert.Equal(1, resumo.Abertas);
        Assert.Equal(cenario.DenunciaId, Assert.Single(fila).Id);
    }

    [Fact]
    public async Task Api_restringe_a_fila_ao_administrador_e_bloqueia_conta_suspensa()
    {
        await using var factory = new TutorClassApiFactory();
        var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin/denuncias")).StatusCode);

        var email = $"susp{Guid.NewGuid():N}@example.test";
        await client.PostAsJsonAsync("/api/auth/registrar/aluno", new RegistrarAlunoRequest("Aluno Suspenso", email, "Senha-ficticia123!", null));
        var login = (await (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "Senha-ficticia123!"))).Content.ReadFromJsonAsync<LoginResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/admin/denuncias")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/denuncias/minhas")).StatusCode);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorClassDbContext>();
            db.Usuarios.Single(u => u.Id == login.Usuario.Id).Desativar();
            await db.SaveChangesAsync();
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/denuncias/minhas")).StatusCode);
    }

    private static async Task<Cenario> SeedDenunciaAsync()
    {
        var db = CreateDbContext();
        var denunciante = Add(db, new Aluno("Ana", NewEmail(), "hash"));
        var alvo = Add(db, new Professor("Prof Bia", NewEmail(), "hash"));
        var admin = Add(db, new Administrador("Admin", NewEmail(), "hash"));
        await db.SaveChangesAsync();
        var resposta = await CreateService(db).CriarAsync(denunciante.Id,
            new CriarDenunciaRequest(TipoAlvoDenuncia.Professor, alvo.Id.ToString(), MotivoDenuncia.ConteudoOfensivo, null), CancellationToken.None);
        return new Cenario(db, resposta.Id, denunciante.Id, alvo.Id, admin.Id);
    }

    private static DenunciaService CreateService(TutorClassDbContext db, IMentoriaChatStore? store = null)
        => new(db, store ?? new FakeChatStore(), new NotificacaoService(db));

    private static TEntity Add<TEntity>(TutorClassDbContext db, TEntity entity) where TEntity : class
    {
        db.Add(entity);
        return entity;
    }

    private static string NewEmail() => $"{Guid.NewGuid():N}@teste.local";

    private static TutorClassDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TutorClassDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TutorClassDbContext(options);
    }

    private sealed record Cenario(TutorClassDbContext Db, Guid DenunciaId, Guid DenuncianteId, Guid AlvoId, Guid AdminId);

    private sealed class FakeChatStore : IMentoriaChatStore
    {
        private readonly IReadOnlyCollection<MensagemMentoriaResponse> _mensagens;

        public FakeChatStore(params MensagemMentoriaResponse[] mensagens)
        {
            _mensagens = mensagens;
        }

        public bool Configurado => true;

        public Task<MentoriaChatResponse> ObterOuCriarChatAsync(MentoriaAlunoProfessor vinculo, CancellationToken cancellationToken)
            => Task.FromResult(new MentoriaChatResponse("chat-1", vinculo.Id, vinculo.AlunoId, vinculo.ProfessorId, false, DateTime.UtcNow));

        public Task<MensagemMentoriaResponse> SalvarMensagemAsync(MentoriaChatResponse chat, Guid remetenteUsuarioId, string remetenteNome, string conteudo, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<IReadOnlyCollection<MensagemMentoriaResponse>> ListarMensagensAsync(Guid vinculoMentoriaId, int limit, DateTime? beforeUtc, CancellationToken cancellationToken)
            => Task.FromResult(_mensagens);

        public Task<int> MarcarComoLidasAsync(Guid vinculoMentoriaId, Guid leitorUsuarioId, CancellationToken cancellationToken)
            => Task.FromResult(0);

        public Task<bool> IsHealthyAsync(CancellationToken cancellationToken) => Task.FromResult(true);
    }
}
