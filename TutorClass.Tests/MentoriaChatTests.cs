using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TutorClass.API.Hubs;
using TutorClass.Application.Abstractions;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;
using TutorClass.Domain.Common;
using TutorClass.Domain.Mentoring;
using TutorClass.Domain.Plans;
using TutorClass.Domain.Subjects;
using TutorClass.Domain.Teachers;
using TutorClass.Domain.Users;
using TutorClass.Infrastructure.Persistence;

namespace TutorClass.Tests;

public sealed class MentoriaChatTests
{
    [Fact]
    public async Task SignalR_websocket_retorna_falha_identificavel_na_invocacao()
    {
        var store = new InMemoryChatStore();
        await using var factory = new TutorClassApiFactory(services =>
        {
            services.RemoveAll<IMentoriaChatStore>(); services.AddSingleton<IMentoriaChatStore>(store);
        });
        var client = factory.CreateClient(); var scenario = await SeedHttpScenarioAsync(factory);
        await AuthorizeAsync(client,"aluno.http.chat@teste.local","TutorClass@123");
        var token = client.DefaultRequestHeaders.Authorization!.Parameter!;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var socket = await factory.Server.CreateWebSocketClient().ConnectAsync(
            new Uri("ws://localhost/hubs/mentoria-chat?access_token="+Uri.EscapeDataString(token)),timeout.Token);
        async Task Send(string json) => await socket.SendAsync(System.Text.Encoding.UTF8.GetBytes(json+"\u001e").AsMemory(),
            System.Net.WebSockets.WebSocketMessageType.Text,true,timeout.Token);
        async Task<string> Receive()
        {
            var bytes = new byte[4096]; var text = new System.Text.StringBuilder();
            System.Net.WebSockets.ValueWebSocketReceiveResult part;
            do
            {
                part=await socket.ReceiveAsync(bytes.AsMemory(),timeout.Token);
                text.Append(System.Text.Encoding.UTF8.GetString(bytes,0,part.Count));
            } while(!part.EndOfMessage);
            return text.ToString();
        }
        await Send("{\"protocol\":\"json\",\"version\":1}");
        Assert.Contains("{}",await Receive());
        await Send(System.Text.Json.JsonSerializer.Serialize(new
        { type=1, invocationId="blocked", target="EnviarMensagem", arguments=new object[]{scenario.VinculoId,"Você é uma idiota"} }));
        var wire=await Receive();
        using var completion=System.Text.Json.JsonDocument.Parse(wire.Split('\u001e')[0]);
        Assert.Equal(3,completion.RootElement.GetProperty("type").GetInt32());
        Assert.Equal("blocked",completion.RootElement.GetProperty("invocationId").GetString());
        Assert.Contains("MENSAGEM_BLOQUEADA|OFENSA_DIRECIONADA|",completion.RootElement.GetProperty("error").GetString());
        Assert.False(completion.RootElement.TryGetProperty("result",out _));
        Assert.DoesNotContain("MensagemRecebida",wire); Assert.DoesNotContain("idiota",wire);
        Assert.Equal(0,store.OpenCalls); Assert.Equal(0,store.SaveCalls); Assert.Empty(store.Messages);
        socket.Abort();
    }

    [Theory]
    [InlineData("Você é uma idiota!", "OFENSA_DIRECIONADA")]
    [InlineData("Professora, sua imbecil!!!", "OFENSA_DIRECIONADA")]
    [InlineData("Vai se foder", "OFENSA_DIRECIONADA")]
    [InlineData("professora vai se foder", "OFENSA_DIRECIONADA")]
    [InlineData("vai se fuder", "OFENSA_DIRECIONADA")]
    [InlineData("Professora, vá se fuder!", "OFENSA_DIRECIONADA")]
    [InlineData("você é uma vagabunda", "OFENSA_DIRECIONADA")]
    [InlineData("vou te bater", "AMEACA_EXPLICITA")]
    [InlineData("Eu irei bater em você amanhã", "AMEACA_EXPLICITA")]
    [InlineData("Vou te matar amanhã", "AMEACA_EXPLICITA")]
    [InlineData("Vou te agredir", "AMEACA_EXPLICITA")]
    public async Task Moderacao_direta_bloqueia_ambos_participantes_antes_do_store(string texto, string categoria)
    {
        await using var db = CreateDbContext(); var scenario = SeedScenario(db);
        var store = new InMemoryChatStore(); var service = new MentoriaChatService(db,store);
        foreach(var sender in new[]{scenario.AlunoId,scenario.ProfessorId})
        {
            var error = await Assert.ThrowsAsync<MensagemMentoriaBloqueadaException>(() =>
                service.EnviarMensagemAsync(scenario.VinculoId,sender,new(texto),default));
            Assert.Equal(categoria,error.Categoria); Assert.DoesNotContain(texto,error.Message);
        }
        Assert.Equal(0,store.OpenCalls); Assert.Equal(0,store.SaveCalls); Assert.Empty(store.Messages);
    }

    [Theory]
    [InlineData("idiota")]
    [InlineData("vagabundo")]
    [InlineData("o que significa vagabundo?")]
    [InlineData("um colega disse que vai me bater. O que faço?")]
    [InlineData("Ela disse: professora vai se foder. Como lidar com isso?")]
    [InlineData("\"vai se fuder\"")]
    [InlineData("O que significa imbecil nesse texto?")]
    [InlineData("Me chamaram de idiota na escola, preciso de ajuda.")]
    [InlineData("Ameaçaram-me dizendo: vou te matar amanhã.")]
    [InlineData("\"Você é uma idiota!\"")]
    [InlineData("Analise a frase 'vou te matar' e o bullying.")]
    [InlineData("Sofri violência e assédio. Posso conversar?")]
    [InlineData("Vou te matar de rir com essa piada")]
    [InlineData("Não diga você é um idiota aos colegas.")]
    public async Task Moderacao_preserva_mensagens_legitimas_e_texto_original(string texto)
    {
        await using var db = CreateDbContext(); var scenario = SeedScenario(db);
        var store = new InMemoryChatStore(); var service = new MentoriaChatService(db,store);
        var saved = await service.EnviarMensagemAsync(scenario.VinculoId,scenario.AlunoId,new(texto),default);
        Assert.Equal(texto,saved.Conteudo); Assert.Equal(scenario.AlunoId,saved.RemetenteUsuarioId);
        Assert.Equal(1,store.OpenCalls); Assert.Equal(1,store.SaveCalls);
    }

    [Fact]
    public async Task Autorizacao_e_tamanho_precedem_moderacao()
    {
        await using var db = CreateDbContext(); var scenario = SeedScenario(db);
        var store = new InMemoryChatStore(); var service = new MentoriaChatService(db,store);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.EnviarMensagemAsync(scenario.VinculoId,Guid.NewGuid(),new("Você é idiota"),default));
        await Assert.ThrowsAsync<DomainException>(() => service.EnviarMensagemAsync(scenario.VinculoId,scenario.AlunoId,new("Você é idiota" + new string('!',2000)),default));
        Assert.Equal(0,store.OpenCalls); Assert.Equal(0,store.SaveCalls);
    }

    [Fact]
    public async Task Hub_bloqueia_sem_emitir_evento_e_permite_reformular()
    {
        await using var db = CreateDbContext(); var scenario = SeedScenario(db);
        var store = new InMemoryChatStore(); var proxy = new FakeClientProxy();
        var hub = new MentoriaChatHub(new MentoriaChatService(db,store))
        { Context = new FakeHubCallerContext(scenario.AlunoId), Clients = new FakeHubCallerClients(proxy) };
        var error = await Assert.ThrowsAsync<HubException>(()=>hub.EnviarMensagem(scenario.VinculoId,"Você é uma idiota"));
        Assert.StartsWith("MENSAGEM_BLOQUEADA|OFENSA_DIRECIONADA|",error.Message);
        Assert.DoesNotContain("idiota",error.Message);
        Assert.Empty(proxy.Invocations); Assert.Empty(store.Messages); Assert.Equal(0,store.OpenCalls); Assert.Equal(0,store.SaveCalls);
        var saved = await hub.EnviarMensagem(scenario.VinculoId,"Professora, preciso de ajuda.");
        Assert.Single(proxy.Invocations); Assert.Single(store.Messages); Assert.Equal(scenario.AlunoId,saved.RemetenteUsuarioId);
    }

    [Fact]
    public async Task Http_bloqueia_sem_persistir_ou_publicar_e_preserva_sucesso()
    {
        var store = new InMemoryChatStore(); var proxy = new FakeClientProxy();
        await using var factory = new TutorClassApiFactory(services =>
        {
            services.RemoveAll<IMentoriaChatStore>(); services.AddSingleton<IMentoriaChatStore>(store);
            services.AddSingleton<IHubContext<MentoriaChatHub>>(new FakeHubContext(proxy));
        });
        var client = factory.CreateClient(); var scenario = await SeedHttpScenarioAsync(factory);
        await AuthorizeAsync(client,"aluno.http.chat@teste.local","TutorClass@123");
        var path=$"/api/mentorias/{scenario.VinculoId}/chat/mensagens";
        var response=await client.PostAsJsonAsync(path,new EnviarMensagemMentoriaRequest("Vou te matar"));
        Assert.Equal(System.Net.HttpStatusCode.BadRequest,response.StatusCode);
        using var body=System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("MENSAGEM_BLOQUEADA",body.RootElement.GetProperty("codigo").GetString());
        Assert.Equal("AMEACA_EXPLICITA",body.RootElement.GetProperty("categoria").GetString());
        Assert.Contains("Mensagem não enviada",body.RootElement.GetProperty("error").GetString());
        Assert.DoesNotContain("Vou te matar",body.RootElement.ToString());
        Assert.Empty(proxy.Invocations); Assert.Empty(store.Messages); Assert.Equal(0,store.OpenCalls); Assert.Equal(0,store.SaveCalls);
        var ok=await client.PostAsJsonAsync(path,new EnviarMensagemMentoriaRequest("Preciso de ajuda com um relato de violência."));
        Assert.Equal(System.Net.HttpStatusCode.Created,ok.StatusCode);
        Assert.Single(proxy.Invocations); Assert.Single(store.Messages);
        Assert.Equal(scenario.AlunoId,(await ok.Content.ReadFromJsonAsync<MensagemMentoriaResponse>())!.RemetenteUsuarioId);
    }

    [Fact]
    public async Task Aluno_e_professor_do_vinculo_acessam_chat()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db);
        var store = new InMemoryChatStore();
        var service = new MentoriaChatService(db, store);

        var chatAluno = await service.ObterChatAsync(scenario.VinculoId, scenario.AlunoId, CancellationToken.None);
        var chatProfessor = await service.ObterChatAsync(scenario.VinculoId, scenario.ProfessorId, CancellationToken.None);

        Assert.Equal(chatAluno.Id, chatProfessor.Id);
        Assert.False(chatAluno.SomenteLeitura);
    }

    [Fact]
    public async Task Outro_aluno_e_outro_professor_nao_acessam_chat()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db);
        var outroAluno = new Aluno("Outro Aluno", "outro.chat@teste.local", "hash");
        var outroProfessor = new Professor("Outro Professor", "outro.prof.chat@teste.local", "hash");
        db.AddRange(outroAluno, outroProfessor);
        await db.SaveChangesAsync();
        var service = new MentoriaChatService(db, new InMemoryChatStore());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ObterChatAsync(scenario.VinculoId, outroAluno.Id, CancellationToken.None));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ObterChatAsync(scenario.VinculoId, outroProfessor.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Vinculo_ativo_libera_mensagem_e_persiste_historico()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db);
        var store = new InMemoryChatStore();
        var service = new MentoriaChatService(db, store);

        var mensagem = await service.EnviarMensagemAsync(scenario.VinculoId, scenario.AlunoId, new EnviarMensagemMentoriaRequest("Oi, professora"), CancellationToken.None);
        var historico = await service.ListarMensagensAsync(scenario.VinculoId, scenario.ProfessorId, 30, null, CancellationToken.None);

        Assert.Equal("Oi, professora", mensagem.Conteudo);
        Assert.Single(historico.Mensagens);
        Assert.Equal(mensagem.Id, historico.Mensagens.Single().Id);
    }

    [Fact]
    public async Task Vinculo_encerrado_bloqueia_novas_mensagens_mas_mantem_historico_somente_leitura()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db);
        var store = new InMemoryChatStore();
        var service = new MentoriaChatService(db, store);
        await service.EnviarMensagemAsync(scenario.VinculoId, scenario.AlunoId, new EnviarMensagemMentoriaRequest("Mensagem antiga"), CancellationToken.None);
        db.MentoriasAlunoProfessor.Single(m => m.Id == scenario.VinculoId).Encerrar(MotivoEncerramentoMentoria.TrocaSolicitadaPeloAluno);
        await db.SaveChangesAsync();

        var chat = await service.ObterChatAsync(scenario.VinculoId, scenario.AlunoId, CancellationToken.None);
        var historico = await service.ListarMensagensAsync(scenario.VinculoId, scenario.AlunoId, 30, null, CancellationToken.None);

        Assert.True(chat.SomenteLeitura);
        Assert.Single(historico.Mensagens);
        await Assert.ThrowsAsync<DomainException>(() => service.EnviarMensagemAsync(scenario.VinculoId, scenario.AlunoId, new EnviarMensagemMentoriaRequest("Nova"), CancellationToken.None));
    }

    [Fact]
    public async Task Aluno_sem_plano_mentoria_nao_envia_mensagem_mesmo_com_vinculo()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db, withPlan: false);
        var service = new MentoriaChatService(db, new InMemoryChatStore());

        await Assert.ThrowsAsync<DomainException>(() => service.EnviarMensagemAsync(scenario.VinculoId, scenario.AlunoId, new EnviarMensagemMentoriaRequest("Ola"), CancellationToken.None));
    }

    [Fact]
    public async Task Historico_nao_mistura_mensagens_de_outro_vinculo()
    {
        await using var db = CreateDbContext();
        var scenarioA = SeedScenario(db, "a");
        var scenarioB = SeedScenario(db, "b");
        var store = new InMemoryChatStore();
        var service = new MentoriaChatService(db, store);

        await service.EnviarMensagemAsync(scenarioA.VinculoId, scenarioA.AlunoId, new EnviarMensagemMentoriaRequest("Mensagem A"), CancellationToken.None);
        await service.EnviarMensagemAsync(scenarioB.VinculoId, scenarioB.AlunoId, new EnviarMensagemMentoriaRequest("Mensagem B"), CancellationToken.None);

        var historico = await service.ListarMensagensAsync(scenarioA.VinculoId, scenarioA.AlunoId, 30, null, CancellationToken.None);

        Assert.Single(historico.Mensagens);
        Assert.Equal("Mensagem A", historico.Mensagens.Single().Conteudo);
    }

    [Fact]
    public async Task Paginacao_respeita_limite_e_cursor_sem_duplicar()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db);
        var store = new InMemoryChatStore();
        var service = new MentoriaChatService(db, store);
        for (var i = 1; i <= 5; i++)
        {
            await service.EnviarMensagemAsync(scenario.VinculoId, scenario.AlunoId, new EnviarMensagemMentoriaRequest($"Mensagem {i}"), CancellationToken.None);
        }

        var primeiraPagina = await service.ListarMensagensAsync(scenario.VinculoId, scenario.AlunoId, 2, null, CancellationToken.None);
        var segundaPagina = await service.ListarMensagensAsync(scenario.VinculoId, scenario.AlunoId, 2, primeiraPagina.ProximoBeforeUtc, CancellationToken.None);

        Assert.Equal(2, primeiraPagina.Mensagens.Count);
        Assert.Equal(2, segundaPagina.Mensagens.Count);
        Assert.Empty(primeiraPagina.Mensagens.Select(m => m.Id).Intersect(segundaPagina.Mensagens.Select(m => m.Id)));
    }

    [Fact]
    public async Task Marcar_como_lidas_atualiza_mensagens_do_outro_participante()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db);
        var service = new MentoriaChatService(db, new InMemoryChatStore());
        await service.EnviarMensagemAsync(scenario.VinculoId, scenario.AlunoId, new EnviarMensagemMentoriaRequest("Para leitura"), CancellationToken.None);

        var result = await service.MarcarComoLidasAsync(scenario.VinculoId, scenario.ProfessorId, CancellationToken.None);
        var historico = await service.ListarMensagensAsync(scenario.VinculoId, scenario.ProfessorId, 30, null, CancellationToken.None);

        Assert.Equal(1, result.QuantidadeMarcada);
        Assert.True(historico.Mensagens.Single().Lida);
        Assert.NotNull(historico.Mensagens.Single().DataLeituraUtc);
    }

    [Fact]
    public async Task Hub_autenticado_ingressa_em_conversa_autorizada()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db);
        var groups = new FakeGroupManager();
        var hub = new MentoriaChatHub(new MentoriaChatService(db, new InMemoryChatStore()))
        {
            Context = new FakeHubCallerContext(scenario.AlunoId),
            Groups = groups
        };

        await hub.EntrarNoChat(scenario.VinculoId);

        Assert.Contains(MentoriaChatHub.GroupName(scenario.VinculoId), groups.Groups);
    }

    [Fact]
    public async Task Hub_nao_autoriza_usuario_fora_do_vinculo()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db);
        var outroUsuarioId = Guid.NewGuid();
        var hub = new MentoriaChatHub(new MentoriaChatService(db, new InMemoryChatStore()))
        {
            Context = new FakeHubCallerContext(outroUsuarioId),
            Groups = new FakeGroupManager()
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => hub.EntrarNoChat(scenario.VinculoId));
    }

    [Fact]
    public async Task Hub_persiste_mensagem_antes_de_publicar()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db);
        var store = new InMemoryChatStore();
        var clientProxy = new FakeClientProxy();
        var hub = new MentoriaChatHub(new MentoriaChatService(db, store))
        {
            Context = new FakeHubCallerContext(scenario.AlunoId),
            Groups = new FakeGroupManager(),
            Clients = new FakeHubCallerClients(clientProxy)
        };

        var mensagem = await hub.EnviarMensagem(scenario.VinculoId, "Mensagem em tempo real");

        Assert.Single(store.Messages);
        Assert.Equal(mensagem.Id, store.Messages.Single().Id);
        Assert.Equal("MensagemRecebida", clientProxy.Invocations.Single());
    }

    [Fact]
    public async Task Usuario_nao_autenticado_nao_acessa_endpoint_chat()
    {
        await using var factory = new TutorClassApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/mentorias/{Guid.NewGuid()}/chat");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Endpoint_http_paginacao_com_cursor_utc_nao_repete_mensagens()
    {
        var store = new InMemoryChatStore();
        await using var factory = new TutorClassApiFactory(services =>
        {
            services.RemoveAll<IMentoriaChatStore>();
            services.AddSingleton<IMentoriaChatStore>(store);
        });
        var client = factory.CreateClient();
        var scenario = await SeedHttpScenarioAsync(factory);
        await AuthorizeAsync(client, "aluno.http.chat@teste.local", "TutorClass@123");

        for (var i = 1; i <= 5; i++)
        {
            var response = await client.PostAsJsonAsync(
                $"/api/mentorias/{scenario.VinculoId}/chat/mensagens",
                new EnviarMensagemMentoriaRequest($"Mensagem HTTP {i}"));
            response.EnsureSuccessStatusCode();
        }

        var primeiraResponse = await client.GetAsync($"/api/mentorias/{scenario.VinculoId}/chat/mensagens?limit=2");
        var primeira = await primeiraResponse.Content.ReadFromJsonAsync<MensagensMentoriaPageResponse>();
        var segundaResponse = await client.GetAsync($"/api/mentorias/{scenario.VinculoId}/chat/mensagens?limit=2&before={Uri.EscapeDataString(primeira!.ProximoBeforeUtc!.Value.ToString("O"))}");
        var segunda = await segundaResponse.Content.ReadFromJsonAsync<MensagensMentoriaPageResponse>();

        Assert.Equal(2, primeira.Mensagens.Count);
        Assert.Equal(2, segunda!.Mensagens.Count);
        Assert.Empty(primeira.Mensagens.Select(m => m.Id).Intersect(segunda.Mensagens.Select(m => m.Id)));
    }

    private static ScenarioChat SeedScenario(TutorClassDbContext db, string suffix = "default", bool withPlan = true)
    {
        var aluno = new Aluno($"Aluno Chat {suffix}", $"aluno.chat.{suffix}.{Guid.NewGuid()}@teste.local", "hash");
        var professor = new Professor($"Professor Chat {suffix}", $"prof.chat.{suffix}.{Guid.NewGuid()}@teste.local", "hash");
        professor.AtualizarPerfilProfissional("Professor mentor para chat.", 100m);
        professor.Aprovar();
        professor.ConfigurarMentoria(true, 5);
        var materia = new Materia($"Materia Chat {suffix} {Guid.NewGuid()}");
        var vinculo = new MentoriaAlunoProfessor(aluno.Id, professor.Id, 1);

        db.AddRange(aluno, professor, materia, vinculo);
        if (withPlan)
        {
            db.Add(new AssinaturaAluno(aluno.Id, aluno.Id, PlanoAluno.Mentoria, 79.99m, 59.99m, true));
        }

        db.SaveChanges();
        return new ScenarioChat(aluno.Id, professor.Id, vinculo.Id);
    }

    private static async Task<ScenarioChat> SeedHttpScenarioAsync(TutorClassApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TutorClassDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>();
        var aluno = new Aluno("Aluno HTTP Chat", "aluno.http.chat@teste.local", "pending");
        aluno.DefinirPasswordHash(hasher.HashPassword(aluno, "TutorClass@123"));
        var professor = new Professor("Professor HTTP Chat", "professor.http.chat@teste.local", "pending");
        professor.DefinirPasswordHash(hasher.HashPassword(professor, "TutorClass@123"));
        professor.AtualizarPerfilProfissional("Professor mentor para teste HTTP de chat.", 100m);
        professor.Aprovar();
        professor.ConfigurarMentoria(true, 5);
        var vinculo = new MentoriaAlunoProfessor(aluno.Id, professor.Id, 1);

        db.AddRange(aluno, professor, vinculo);
        db.Add(new AssinaturaAluno(aluno.Id, aluno.Id, PlanoAluno.Mentoria, 79.99m, 59.99m, true));
        await db.SaveChangesAsync();

        return new ScenarioChat(aluno.Id, professor.Id, vinculo.Id);
    }

    private static async Task AuthorizeAsync(HttpClient client, string email, string senha)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, senha));
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
    }

    private static TutorClassDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TutorClassDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TutorClassDbContext(options);
    }

    private sealed record ScenarioChat(Guid AlunoId, Guid ProfessorId, Guid VinculoId);

    private sealed class InMemoryChatStore : Application.Abstractions.IMentoriaChatStore
    {
        private readonly List<MentoriaChatResponse> _chats = [];
        public List<MensagemMentoriaResponse> Messages { get; } = [];
        public int OpenCalls { get; private set; }
        public int SaveCalls { get; private set; }
        public bool Configurado => true;

        public Task<MentoriaChatResponse> ObterOuCriarChatAsync(MentoriaAlunoProfessor vinculo, CancellationToken cancellationToken)
        {
            OpenCalls++;
            var chat = _chats.SingleOrDefault(c => c.VinculoMentoriaId == vinculo.Id);
            if (chat is null)
            {
                chat = new MentoriaChatResponse(Guid.NewGuid().ToString("N"), vinculo.Id, vinculo.AlunoId, vinculo.ProfessorId, !vinculo.Ativo, DateTime.UtcNow);
                _chats.Add(chat);
            }

            return Task.FromResult(chat with { SomenteLeitura = !vinculo.Ativo });
        }

        public Task<MensagemMentoriaResponse> SalvarMensagemAsync(MentoriaChatResponse chat, Guid remetenteUsuarioId, string remetenteNome, string conteudo, CancellationToken cancellationToken)
        {
            SaveCalls++;
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
            var count = 0;
            for (var i = 0; i < Messages.Count; i++)
            {
                var message = Messages[i];
                if (message.VinculoMentoriaId == vinculoMentoriaId && message.RemetenteUsuarioId != leitorUsuarioId && !message.Lida)
                {
                    Messages[i] = message with { Lida = true, DataLeituraUtc = DateTime.UtcNow };
                    count++;
                }
            }

            return Task.FromResult(count);
        }

        public Task<bool> IsHealthyAsync(CancellationToken cancellationToken) => Task.FromResult(true);
    }

    private sealed class FakeHubCallerContext : HubCallerContext
    {
        private readonly ClaimsPrincipal _user;

        public FakeHubCallerContext(Guid userId)
        {
            ConnectionId = Guid.NewGuid().ToString("N");
            _user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "Test"));
        }

        public override string ConnectionId { get; }
        public override string? UserIdentifier => null;
        public override ClaimsPrincipal? User => _user;
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

    private sealed class FakeHubCallerClients : IHubCallerClients, IHubClients
    {
        private readonly IClientProxy _proxy;

        public FakeHubCallerClients(IClientProxy proxy)
        {
            _proxy = proxy;
        }

        public IClientProxy All => _proxy;
        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => _proxy;
        public IClientProxy Caller => _proxy;
        public IClientProxy Others => _proxy;
        public IClientProxy Client(string connectionId) => _proxy;
        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => _proxy;
        public IClientProxy Group(string groupName) => _proxy;
        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => _proxy;
        public IClientProxy Groups(IReadOnlyList<string> groupNames) => _proxy;
        public IClientProxy User(string userId) => _proxy;
        public IClientProxy Users(IReadOnlyList<string> userIds) => _proxy;
        public IClientProxy OthersInGroup(string groupName) => _proxy;
    }

    private sealed class FakeHubContext(IClientProxy proxy) : IHubContext<MentoriaChatHub>
    {
        public IHubClients Clients { get; } = new FakeHubCallerClients(proxy);
        public IGroupManager Groups { get; } = new FakeGroupManager();
    }

    private sealed class FakeClientProxy : IClientProxy
    {
        public List<string> Invocations { get; } = [];

        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            Invocations.Add(method);
            return Task.CompletedTask;
        }
    }
}
