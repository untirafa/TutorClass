using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TutorClass.API.Hubs;
using TutorClass.Application.Abstractions;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;
using TutorClass.Domain.Common;
using TutorClass.Domain.Financial;
using TutorClass.Domain.Lessons;
using TutorClass.Domain.Mentoring;
using TutorClass.Domain.Notifications;
using TutorClass.Domain.Plans;
using TutorClass.Domain.Subjects;
using TutorClass.Domain.Teachers;
using TutorClass.Domain.Users;
using TutorClass.Infrastructure.Persistence;

namespace TutorClass.Tests;

public sealed class ResponsavelPermissoesTests
{
    private const string VideoUrl = "https://www.w3schools.com/html/mov_bbb.mp4";

    [Fact]
    public async Task Responsavel_vinculado_lista_apenas_seus_alunos()
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db);
        var service = new ResponsavelAlunoService(db);

        var alunos = await service.ListarAlunosVinculadosAsync(cenario.ResponsavelId, CancellationToken.None);

        var aluno = Assert.Single(alunos);
        Assert.Equal(cenario.AlunoId, aluno.Id);
        Assert.Equal("Mae", aluno.Parentesco);
    }

    [Fact]
    public async Task Responsavel_nao_vinculado_recebe_forbidden_ao_trocar_aluno_id()
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db);
        var service = new ResponsavelAlunoService(db);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.ValidarVinculoAsync(cenario.ResponsavelNaoVinculadoId, cenario.AlunoId, CancellationToken.None));
    }

    [Fact]
    public async Task Responsavel_vinculado_solicita_aula_sem_impersonar_aluno()
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db);
        var aulaService = new AulaService(db);

        var aula = await aulaService.SolicitarParaAlunoAsync(
            cenario.AlunoId,
            cenario.ResponsavelId,
            CreateRequest(cenario),
            CancellationToken.None);

        Assert.Equal(cenario.AlunoId, aula.Aluno.Id);
        Assert.Equal(cenario.ResponsavelId, aula.SolicitadaPorUsuarioId);
        Assert.Equal(cenario.ResponsavelId, db.Aulas.Single(a => a.Id == aula.Id).SolicitadaPorUsuarioId);
    }

    [Fact]
    public async Task Responsavel_nao_vinculado_nao_solicita_aula_para_aluno()
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db);
        var aulaService = new AulaService(db);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            aulaService.SolicitarParaAlunoAsync(cenario.AlunoId, cenario.ResponsavelNaoVinculadoId, CreateRequest(cenario), CancellationToken.None));
    }

    [Fact]
    public async Task Responsavel_vinculado_lista_reagenda_e_cancela_aulas_do_aluno()
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db);
        var aulaService = new AulaService(db);
        var responsavelService = new ResponsavelAlunoService(db);
        var aula = await aulaService.SolicitarParaAlunoAsync(cenario.AlunoId, cenario.ResponsavelId, CreateRequest(cenario), CancellationToken.None);

        var aulas = await responsavelService.ListarAulasAsync(cenario.ResponsavelId, cenario.AlunoId, CancellationToken.None);
        var reagendada = await aulaService.ReagendarAsync(aula.Id, cenario.ResponsavelId, new ReagendarAulaRequest(cenario.InicioUtc.AddDays(7), null), CancellationToken.None);
        var cancelada = await aulaService.CancelarAsync(aula.Id, cenario.ResponsavelId, "Compromisso familiar.", CancellationToken.None);

        Assert.Contains(aulas, a => a.Id == aula.Id);
        Assert.Equal(cenario.InicioUtc.AddDays(7), reagendada.Inicio);
        Assert.Equal(StatusAula.Cancelada, cancelada.Status);
        Assert.Equal(cenario.ResponsavelId, db.Aulas.Single(a => a.Id == aula.Id).CanceladaPorUsuarioId);
        Assert.Contains(db.AulasReagendamentos, r => r.AulaId == aula.Id && r.SolicitadoPorUsuarioId == cenario.ResponsavelId);
    }

    [Fact]
    public async Task Responsavel_nao_vinculado_nao_reagenda_nem_cancela_aula()
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db);
        var aulaService = new AulaService(db);
        var aula = await aulaService.SolicitarAsync(cenario.AlunoId, CreateRequest(cenario), CancellationToken.None);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            aulaService.ReagendarAsync(aula.Id, cenario.ResponsavelNaoVinculadoId, new ReagendarAulaRequest(cenario.InicioUtc.AddDays(7), null), CancellationToken.None));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            aulaService.CancelarAsync(aula.Id, cenario.ResponsavelNaoVinculadoId, null, CancellationToken.None));
    }

    [Fact]
    public async Task Responsavel_vinculado_paga_aula_e_registra_pagador_e_beneficiario()
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db);
        var aulaService = new AulaService(db);
        var financeiro = new FinanceiroService(db);
        var aula = await aulaService.SolicitarAsync(cenario.AlunoId, CreateRequest(cenario), CancellationToken.None);

        var pagamento = await financeiro.PagarAulaAsync(aula.Id, cenario.ResponsavelId, FormaPagamento.Pix, CancellationToken.None);

        Assert.Equal(cenario.ResponsavelId, pagamento.PagadorUsuarioId);
        Assert.Equal(cenario.AlunoId, pagamento.AlunoId);
    }

    [Fact]
    public async Task Responsavel_nao_vinculado_nao_paga_aula_do_aluno()
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db);
        var aulaService = new AulaService(db);
        var financeiro = new FinanceiroService(db);
        var aula = await aulaService.SolicitarAsync(cenario.AlunoId, CreateRequest(cenario), CancellationToken.None);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            financeiro.PagarAulaAsync(aula.Id, cenario.ResponsavelNaoVinculadoId, FormaPagamento.Pix, CancellationToken.None));
    }

    [Fact]
    public async Task Responsavel_vinculado_consulta_plano_e_assina_para_beneficiario()
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db, withPlan: false);
        var planos = new PlanoAlunoService(db);

        var statusAntes = await planos.ObterStatusParaUsuarioAsync(cenario.ResponsavelId, cenario.AlunoId, CancellationToken.None);
        var assinatura = await planos.AssinarParaBeneficiarioAsync(
            cenario.ResponsavelId,
            cenario.AlunoId,
            new AssinarPlanoRequest(PlanoAluno.Foco, cenario.AlunoId),
            CancellationToken.None);

        Assert.Equal(PlanoAluno.Gratuito, statusAntes.PlanoAtual);
        Assert.Equal(cenario.ResponsavelId, assinatura.PagadorUsuarioId);
        Assert.Equal(cenario.AlunoId, assinatura.AlunoId);
    }

    [Fact]
    public async Task Responsavel_nao_vinculado_nao_consulta_plano()
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db);
        var planos = new PlanoAlunoService(db);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            planos.ObterStatusParaUsuarioAsync(cenario.ResponsavelNaoVinculadoId, cenario.AlunoId, CancellationToken.None));
    }

    [Fact]
    public async Task Responsavel_ve_mentores_e_acompanhamento_resumido_sem_conteudo_pedagogico()
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db);
        var service = new ResponsavelAlunoService(db);

        var mentorias = await service.ListarMentoriasAsync(cenario.ResponsavelId, cenario.AlunoId, CancellationToken.None);
        var acompanhamentos = await service.ListarAcompanhamentosResumidosAsync(cenario.ResponsavelId, cenario.AlunoId, CancellationToken.None);

        var mentoria = Assert.Single(mentorias);
        var acompanhamento = Assert.Single(acompanhamentos);
        Assert.Equal(cenario.ProfessorId, mentoria.ProfessorId);
        Assert.Equal(cenario.AcompanhamentoId, acompanhamento.Id);
        Assert.Equal(StatusAcompanhamentoMentoria.Concluido, acompanhamento.Status);
    }

    [Fact]
    public async Task Responsavel_nao_altera_mentor_do_aluno()
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db);
        var mentoria = new MentoriaService(db);

        await Assert.ThrowsAsync<DomainException>(() =>
            mentoria.TrocarMentorAsync(cenario.ResponsavelId, 1, new TrocarMentorRequest(cenario.OutroProfessorId), CancellationToken.None));
    }

    [Fact]
    public async Task Responsavel_vinculado_nao_le_historico_privado()
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db);
        var store = new InMemoryChatStore();
        var chat = new MentoriaChatService(db, store);
        await chat.EnviarMensagemAsync(cenario.VinculoMentoriaId, cenario.AlunoId, new EnviarMensagemMentoriaRequest("Oi, mentor"), CancellationToken.None);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => chat.ListarMensagensParaResponsavelAsync(cenario.VinculoMentoriaId, cenario.ResponsavelId, 30, null, CancellationToken.None));



    }

    [Fact]
    public async Task Responsavel_nao_vinculado_nao_le_chat_e_responsavel_nao_envia_mensagem()
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db);
        var chat = new MentoriaChatService(db, new InMemoryChatStore());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            chat.ListarMensagensParaResponsavelAsync(cenario.VinculoMentoriaId, cenario.ResponsavelNaoVinculadoId, 30, null, CancellationToken.None));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            chat.EnviarMensagemAsync(cenario.VinculoMentoriaId, cenario.ResponsavelId, new EnviarMensagemMentoriaRequest("Mensagem indevida"), CancellationToken.None));
    }

    [Fact]
    public async Task Responsavel_nao_entra_no_signalr_do_chat()
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db);
        var groups = new FakeGroupManager();
        var hub = new MentoriaChatHub(new MentoriaChatService(db, new InMemoryChatStore()))
        {
            Context = new FakeHubCallerContext(cenario.ResponsavelId),
            Groups = groups
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => hub.EntrarNoChat(cenario.VinculoMentoriaId));
        Assert.Empty(groups.Groups);
    }

    [Fact]
    public async Task Historico_de_chat_encerrado_nao_e_exposto_ao_responsavel()
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db);
        var store = new InMemoryChatStore();
        var chat = new MentoriaChatService(db, store);
        await chat.EnviarMensagemAsync(cenario.VinculoMentoriaId, cenario.AlunoId, new EnviarMensagemMentoriaRequest("Historico preservado"), CancellationToken.None);
        db.MentoriasAlunoProfessor.Single(m => m.Id == cenario.VinculoMentoriaId).Encerrar(MotivoEncerramentoMentoria.TrocaSolicitadaPeloAluno);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => chat.ListarMensagensParaResponsavelAsync(cenario.VinculoMentoriaId, cenario.ResponsavelId, 30, null, CancellationToken.None));


    }

    [Fact]
    public async Task Responsavel_ve_resumo_de_gravacao_mas_nao_acessa_url()
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db);
        var gravacaoService = new GravacaoAulaService(db);
        var responsavelService = new ResponsavelAlunoService(db);
        var aula = db.Aulas.Single(a => a.Id == cenario.AulaId);
        var gravacao = await gravacaoService.CriarAsync(aula.Id, cenario.AdminId, administrador: true, CreateRecordingRequest(), CancellationToken.None);

        var resumo = await responsavelService.ObterResumoGravacaoAsync(cenario.ResponsavelId, cenario.AlunoId, aula.Id, CancellationToken.None);

        Assert.True(resumo.GravacaoDisponivel);
        Assert.Equal(gravacao.Titulo, resumo.Titulo);
        await Assert.ThrowsAsync<DomainException>(() =>
            gravacaoService.BuscarParaAlunoAsync(aula.Id, cenario.ResponsavelId, CancellationToken.None));
    }

    [Fact]
    public async Task Responsavel_consulta_apenas_suas_notificacoes()
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db);
        var service = new NotificacaoService(db);
        service.CriarSeNaoExistir(cenario.ResponsavelId, TipoNotificacao.PagamentoConfirmado, "Pagamento confirmado", "Pagamento aprovado.", "/financeiro", cenario.AulaId, "RESPONSAVEL:PAGAMENTO");
        service.CriarSeNaoExistir(cenario.AlunoId, TipoNotificacao.AulaConfirmada, "Aula confirmada", "Sua aula foi confirmada.", "/minhas-aulas", cenario.AulaId, "ALUNO:AULA");
        await db.SaveChangesAsync();

        var notificacoes = await service.ListarAsync(cenario.ResponsavelId, false, 1, 20, CancellationToken.None);

        var notificacao = Assert.Single(notificacoes);
        Assert.Equal("Pagamento confirmado", notificacao.Titulo);
    }

    [Fact]
    public async Task Responsavel_nao_usa_tutoria_nem_consumo_do_aluno()
    {
        await using var db = CreateDbContext();
        var cenario = SeedScenario(db);
        var service = new TutorIaService(db, new FakeTutorIaProvider());

        await Assert.ThrowsAsync<DomainException>(() =>
            service.ConversarAsync(cenario.ResponsavelId, new TutorIaChatRequest("Explique biologia"), CancellationToken.None));
        Assert.Empty(db.UsosIaAluno);
    }

    [Fact]
    public async Task Endpoints_de_aluno_bloqueiam_responsavel_para_gravacao_e_tutoria()
    {
        await using var factory = new TutorClassApiFactory();
        var client = factory.CreateClient();
        await SeedResponsibleUserAsync(factory);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, "responsavel.endpoint@teste.local"));

        var gravacao = await client.GetAsync($"/api/aulas/{Guid.NewGuid()}/gravacao");
        var tutoria = await client.GetAsync("/api/tutoria/status");
        var chat = await client.PostAsJsonAsync("/api/tutoria/chat", new { mensagem = "Explique biologia" });
        Assert.Equal(HttpStatusCode.Forbidden, chat.StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, gravacao.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, tutoria.StatusCode);
    }

    private static ScenarioResponsavel SeedScenario(TutorClassDbContext db, bool withPlan = true)
    {
        var admin = new Administrador("Admin", $"admin.responsavel.{Guid.NewGuid()}@teste.local", "hash");
        var aluno = new Aluno("Aluno Vinculado", $"aluno.responsavel.{Guid.NewGuid()}@teste.local", "hash", new DateOnly(2011, 5, 1));
        var responsavel = new Responsavel("Responsavel Vinculado", $"responsavel.{Guid.NewGuid()}@teste.local", "hash", "11999999999");
        var responsavelNaoVinculado = new Responsavel("Responsavel Nao Vinculado", $"responsavel.nv.{Guid.NewGuid()}@teste.local", "hash", "11888888888");
        var materia = new Materia($"Biologia Responsavel {Guid.NewGuid()}");
        var professor = SeedProfessor("Professor Mentor", $"prof.responsavel.{Guid.NewGuid()}@teste.local");
        var outroProfessor = SeedProfessor("Outro Professor Mentor", $"outro.prof.responsavel.{Guid.NewGuid()}@teste.local");
        var candidaturaProfessor = new CandidaturaProfessor(professor.Id, "Candidatura aprovada.");
        candidaturaProfessor.IniciarAnalise(admin.Id);
        candidaturaProfessor.Aprovar(admin.Id);
        var candidaturaOutroProfessor = new CandidaturaProfessor(outroProfessor.Id, "Candidatura aprovada.");
        candidaturaOutroProfessor.IniciarAnalise(admin.Id);
        candidaturaOutroProfessor.Aprovar(admin.Id);
        var inicio = NextUtc(DayOfWeek.Monday, 14);
        var aula = new Aula(aluno.Id, professor.Id, materia.Id, inicio, 60, 100m);
        aula.Confirmar(professor.Id);
        aula.Concluir(professor.Id);
        var vinculo = new MentoriaAlunoProfessor(aluno.Id, professor.Id, 1);
        var acompanhamento = new AcompanhamentoMentoria(aula.Id, aluno.Id, professor.Id, vinculo.Id);
        acompanhamento.Atualizar("Resumo privado", "Ponto forte privado", "Ponto a melhorar privado", "Proximos passos privados", "Meta privada", "Observacao privada");
        acompanhamento.Concluir();

        db.AddRange(admin, aluno, responsavel, responsavelNaoVinculado, professor, outroProfessor, candidaturaProfessor, candidaturaOutroProfessor, materia, aula, vinculo, acompanhamento);
        db.AddRange(
            new ResponsavelAluno(responsavel.Id, aluno.Id, "Mae"),
            new ProfessorMateria(professor.Id, materia.Id),
            new ProfessorMateria(outroProfessor.Id, materia.Id),
            new DisponibilidadeProfessor(professor.Id, inicio.DayOfWeek, TimeOnly.FromDateTime(inicio), TimeOnly.FromDateTime(inicio.AddHours(4))),
            new DisponibilidadeProfessor(professor.Id, inicio.AddDays(7).DayOfWeek, TimeOnly.FromDateTime(inicio.AddDays(7)), TimeOnly.FromDateTime(inicio.AddDays(7).AddHours(4))));

        if (withPlan)
        {
            db.Add(new AssinaturaAluno(aluno.Id, responsavel.Id, PlanoAluno.Mentoria, 79.99m, 59.99m, true));
        }

        db.SaveChanges();
        return new ScenarioResponsavel(
            admin.Id,
            aluno.Id,
            responsavel.Id,
            responsavelNaoVinculado.Id,
            professor.Id,
            outroProfessor.Id,
            materia.Id,
            aula.Id,
            vinculo.Id,
            acompanhamento.Id,
            inicio.AddDays(7));
    }

    private static Professor SeedProfessor(string nome, string email)
    {
        var professor = new Professor(nome, email, "hash");
        professor.AtualizarPerfilProfissional("Professor aprovado para testes.", 100m);
        professor.Aprovar();
        professor.ConfigurarMentoria(true, 5);
        return professor;
    }

    private static SolicitarAulaRequest CreateRequest(ScenarioResponsavel cenario)
    {
        return new SolicitarAulaRequest(cenario.ProfessorId, cenario.MateriaId, cenario.InicioUtc, 60);
    }

    private static CriarGravacaoAulaRequest CreateRecordingRequest()
    {
        return new CriarGravacaoAulaRequest("Gravacao da aula", VideoUrl, 1800);
    }

    private static DateTime NextUtc(DayOfWeek dayOfWeek, int hour)
    {
        var date = DateTime.UtcNow.Date.AddDays(2);
        while (date.DayOfWeek != dayOfWeek)
        {
            date = date.AddDays(1);
        }

        return DateTime.SpecifyKind(date.AddHours(hour), DateTimeKind.Utc);
    }

    private static TutorClassDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TutorClassDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TutorClassDbContext(options);
    }

    private static async Task SeedResponsibleUserAsync(TutorClassApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TutorClassDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.IPasswordHasher<Usuario>>();
        var responsavel = new Responsavel("Responsavel Endpoint", "responsavel.endpoint@teste.local", "pending");
        responsavel.DefinirPasswordHash(hasher.HashPassword(responsavel, "TutorClass@123"));
        db.Add(responsavel);
        await db.SaveChangesAsync();
    }

    private static async Task<string> LoginAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "TutorClass@123"));
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return login!.AccessToken;
    }

    private sealed record ScenarioResponsavel(
        Guid AdminId,
        Guid AlunoId,
        Guid ResponsavelId,
        Guid ResponsavelNaoVinculadoId,
        Guid ProfessorId,
        Guid OutroProfessorId,
        Guid MateriaId,
        Guid AulaId,
        Guid VinculoMentoriaId,
        Guid AcompanhamentoId,
        DateTime InicioUtc);

    private sealed class InMemoryChatStore : IMentoriaChatStore
    {
        private readonly List<MentoriaChatResponse> _chats = [];
        public List<MensagemMentoriaResponse> Messages { get; } = [];
        public bool Configurado => true;

        public Task<MentoriaChatResponse> ObterOuCriarChatAsync(MentoriaAlunoProfessor vinculo, CancellationToken cancellationToken)
        {
            var chat = _chats.SingleOrDefault(c => c.VinculoMentoriaId == vinculo.Id);
            if (chat is null)
            {
                chat = new MentoriaChatResponse(Guid.NewGuid().ToString("N"), vinculo.Id, vinculo.AlunoId, vinculo.ProfessorId, !vinculo.Ativo, DateTime.UtcNow);
                _chats.Add(chat);
            }

            return Task.FromResult(chat);
        }

        public Task<MensagemMentoriaResponse> SalvarMensagemAsync(MentoriaChatResponse chat, Guid remetenteUsuarioId, string remetenteNome, string conteudo, CancellationToken cancellationToken)
        {
            var message = new MensagemMentoriaResponse(Guid.NewGuid().ToString("N"), chat.Id, chat.VinculoMentoriaId, remetenteUsuarioId, remetenteNome, conteudo, DateTime.UtcNow.AddMilliseconds(Messages.Count), false, null);
            Messages.Add(message);
            return Task.FromResult(message);
        }

        public Task<IReadOnlyCollection<MensagemMentoriaResponse>> ListarMensagensAsync(Guid vinculoMentoriaId, int limit, DateTime? beforeUtc, CancellationToken cancellationToken)
        {
            var messages = Messages
                .Where(m => m.VinculoMentoriaId == vinculoMentoriaId && (beforeUtc is null || m.DataEnvioUtc < beforeUtc.Value))
                .OrderByDescending(m => m.DataEnvioUtc)
                .Take(limit)
                .OrderBy(m => m.DataEnvioUtc)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<MensagemMentoriaResponse>>(messages);
        }

        public Task<int> MarcarComoLidasAsync(Guid vinculoMentoriaId, Guid leitorUsuarioId, CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }

        public Task<bool> IsHealthyAsync(CancellationToken cancellationToken) => Task.FromResult(true);
    }

    private sealed class FakeTutorIaProvider : ITutorIaProvider
    {
        public Task<string> ResponderAsync(TutorIaProviderRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult("Resposta simulada.");
        }
    }

    private sealed class FakeHubCallerContext : HubCallerContext
    {
        private readonly System.Security.Claims.ClaimsPrincipal _user;

        public FakeHubCallerContext(Guid userId)
        {
            ConnectionId = Guid.NewGuid().ToString("N");
            _user = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(
                [new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString())],
                "Test"));
        }

        public override string ConnectionId { get; }
        public override string? UserIdentifier => null;
        public override System.Security.Claims.ClaimsPrincipal? User => _user;
        public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();
        public override IFeatureCollection Features { get; } = new FeatureCollection();
        public override CancellationToken ConnectionAborted => CancellationToken.None;
        public override void Abort()
        {
        }
    }

    private sealed class FakeGroupManager : IGroupManager
    {
        public List<string> Groups { get; } = [];

        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            Groups.Add(groupName);
            return Task.CompletedTask;
        }

        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            Groups.Remove(groupName);
            return Task.CompletedTask;
        }
    }
}
