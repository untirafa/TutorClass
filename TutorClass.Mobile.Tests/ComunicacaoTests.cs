using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TutorClass.Mobile.Models;
using TutorClass.Mobile.Services;
using TutorClass.Mobile.ViewModels;

namespace TutorClass.Mobile.Tests;

public sealed class ComunicacaoTests
{
    private static HttpResponseMessage Json<T>(T body) => new(HttpStatusCode.OK) { Content = JsonContent.Create(body) };
    private static HttpClient Client(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        => new(new Handler(handler)) { BaseAddress = new("https://tutorclass.onrender.com/") };
    private static SessionService Session(HttpClient http, SessionTests.Store? store = null) => new(http, store ?? new() { Token = "token" });
    private static TutorIaResponse Status(bool access = true, bool limit = false, string? answer = null)
        => new() { PossuiAcesso = access, LimiteAtingido = limit, Plano = "Foco", LimiteDiario = 7, UsadasHoje = 6, RestantesHoje = 1, Resposta = answer, Mensagem = "Mensagem do backend" };

    [Fact]
    public async Task TutorIaEnviaSomenteMensagemComJwtEMostraRespostaMesmoAoAtingirLimite()
    {
        using var http = Client(async req =>
        {
            Assert.Equal("Bearer", req.Headers.Authorization?.Scheme);
            Assert.Equal("token", req.Headers.Authorization?.Parameter);
            if (req.Method == HttpMethod.Get) return Json(Status());
            Assert.Equal("/api/tutoria/chat/stream", req.RequestUri!.AbsolutePath);
            using var json = JsonDocument.Parse(await req.Content!.ReadAsStringAsync());
            Assert.Single(json.RootElement.EnumerateObject());
            Assert.Equal("Explique classes", json.RootElement.GetProperty("mensagem").GetString());
            return Json(new TutorIaResponse { PossuiAcesso = false, LimiteAtingido = true, Plano = "Foco", UsadasHoje = 7, RestantesHoje = 0, LimiteDiario = 7, Resposta = "Uma classe descreve objetos.", Mensagem = "Limite atingido" });
        });
        var vm = new TutorIaViewModel(new(Session(http)), new SessionTests.Navigation()) { Mensagem = "Explique classes" };
        Assert.False(vm.PodeEnviar);
        await vm.CarregarAsync();
        Assert.True(vm.PodeEnviar);
        await vm.EnviarAsync();
        Assert.Equal(2, vm.Mensagens.Count);
        Assert.Equal("Você", vm.Mensagens[0].Autor);
        Assert.Equal("Uma classe descreve objetos.", vm.Mensagens[1].Texto);
        Assert.False(vm.PodeEnviar);
        Assert.Contains("Restantes: 0", vm.Quota);
        Assert.Equal("Limite atingido", vm.Aviso);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task BloqueioVemDaApi(bool access, bool limit)
    {
        int calls = 0;
        using var http = Client(_ => { calls++; return Task.FromResult(Json(Status(access, limit))); });
        var vm = new TutorIaViewModel(new(Session(http)), new SessionTests.Navigation()) { Mensagem = "Oi" };
        await vm.CarregarAsync();
        await vm.EnviarAsync();
        Assert.Equal(1, calls);
        Assert.Empty(vm.Mensagens);
        Assert.Equal("Mensagem do backend", vm.Aviso);
    }

    [Fact]
    public async Task IndisponibilidadeHttp200PreservaQuotaDoBackendESemRespostaInventada()
    {
        using var http = Client(req => Task.FromResult(Json(req.Method == HttpMethod.Get ? Status() : new TutorIaResponse
        {
            PossuiAcesso = false, Motivo = "ERRO_TUTORIA", Mensagem = "Temporariamente indisponível",
            Plano = "Foco", UsadasHoje = 6, RestantesHoje = 1, LimiteDiario = 7
        })));
        var vm = new TutorIaViewModel(new(Session(http)), new SessionTests.Navigation()) { Mensagem = "Oi" };
        await vm.CarregarAsync();
        await vm.EnviarAsync();
        Assert.Single(vm.Mensagens);
        Assert.Equal(6, vm.Status!.UsadasHoje);
        Assert.Equal("Temporariamente indisponível", vm.Aviso);
        Assert.Equal("Oi", vm.Mensagem);
        Assert.False(vm.PodeEnviar);
        await vm.CarregarAsync();
        Assert.True(vm.PodeEnviar);
    }

    [Fact]
    public async Task FalhaDeTransporteNaoRepeteChatEExigeNovoStatus()
    {
        int posts = 0;
        using var http = Client(req =>
        {
            if (req.Method == HttpMethod.Get) return Task.FromResult(Json(Status()));
            posts++;
            throw new TaskCanceledException();
        });
        var vm = new TutorIaViewModel(new(Session(http)), new SessionTests.Navigation()) { Mensagem = "Oi" };
        await vm.CarregarAsync();
        await vm.EnviarAsync();
        await vm.EnviarAsync();
        Assert.Equal(1, posts);
        Assert.True(vm.HasError);
        Assert.Null(vm.Status);
        Assert.False(vm.PodeEnviar);
    }

    [Fact]
    public async Task SemCalculoDeQuotaLocal()
    {
        using var http = Client(_ => Task.FromResult(Json(new TutorIaResponse { Plano = "Mentoria", PossuiAcesso = true, UsadasHoje = 900 })));
        var vm = new TutorIaViewModel(new(Session(http)), new SessionTests.Navigation()) { Mensagem = "Oi" };
        await vm.CarregarAsync();
        Assert.True(vm.PodeEnviar);
        Assert.DoesNotContain("Limite diário", vm.Quota);
        Assert.DoesNotContain("Restantes", vm.Quota);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SessaoExpiradaVoltaAoLogin(bool ia)
    {
        var nav = new SessionTests.Navigation();
        nav.ShowHome();
        var store = new SessionTests.Store { Token = "expired" };
        using var http = Client(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)));
        var session = Session(http, store);
        if (ia) await new TutorIaViewModel(new(session), nav).CarregarAsync();
        else await new NotificacoesViewModel(new(session), nav).CarregarAsync();
        Assert.Null(store.Token);
        Assert.Equal("Login", nav.Page);
    }

    [Fact]
    public async Task NotificacoesListaResumoELidaUsamRetornoReal()
    {
        var id = Guid.NewGuid();
        var read = false;
        using var http = Client(req =>
        {
            Assert.Equal("Bearer", req.Headers.Authorization?.Scheme);
            if (req.RequestUri!.AbsolutePath.EndsWith("marcar-lida"))
            {
                Assert.Equal(HttpMethod.Post, req.Method);
                Assert.EndsWith($"{id}/marcar-lida", req.RequestUri.AbsolutePath);
                read = true;
                return Task.FromResult(Json(new NotificacaoDto { Id = id, Lida = true, Titulo = "Aula" }));
            }
            if (req.RequestUri.AbsolutePath.EndsWith("resumo")) return Task.FromResult(Json(new NotificacaoResumoDto(read ? 0 : 1)));
            return Task.FromResult(Json(new[] { new NotificacaoDto { Id = id, Titulo = "Aula", Mensagem = "Confirmada" } }));
        });
        var vm = new NotificacoesViewModel(new(Session(http)), new SessionTests.Navigation());
        await vm.CarregarAsync();
        Assert.Equal(1, vm.NaoLidas);
        Assert.Equal("Não lida", Assert.Single(vm.Itens).Leitura);
        await vm.MarcarAsync(vm.Itens[0]);
        Assert.True(vm.Itens[0].Lida);
        Assert.Equal(0, vm.NaoLidas);
    }

    [Fact]
    public async Task NotificacoesTodasEPaginacao()
    {
        var items = Enumerable.Range(0, 20).Select(_ => new NotificacaoDto { Id = Guid.NewGuid() }).ToArray();
        bool all = false;
        using var http = Client(req =>
        {
            var path = req.RequestUri!.AbsolutePath;
            if (path.EndsWith("marcar-todas-lidas")) { all = true; Assert.Equal(HttpMethod.Post, req.Method); return Task.FromResult(Json(new MarcarTodasLidasDto(20))); }
            if (path.EndsWith("resumo")) return Task.FromResult(Json(new NotificacaoResumoDto(all ? 0 : 20)));
            if (req.RequestUri.Query.Contains("page=2")) return Task.FromResult(Json(Array.Empty<NotificacaoDto>()));
            return Task.FromResult(Json(items.Select(n => new NotificacaoDto { Id = n.Id, Lida = all }).ToArray()));
        });
        var vm = new NotificacoesViewModel(new(Session(http)), new SessionTests.Navigation());
        await vm.CarregarAsync();
        Assert.True(vm.HasMore);
        await vm.MaisAsync();
        Assert.False(vm.HasMore);
        Assert.Equal(20, vm.Itens.Length);
        await vm.MarcarTodasAsync();
        Assert.All(vm.Itens, n => Assert.True(n.Lida));
        Assert.Equal(0, vm.NaoLidas);
    }

    [Fact]
    public async Task NotificacoesVazias()
    {
        using var http = Client(req => Task.FromResult(req.RequestUri!.AbsolutePath.EndsWith("resumo")
            ? Json(new NotificacaoResumoDto(0)) : Json(Array.Empty<NotificacaoDto>())));
        var vm = new NotificacoesViewModel(new(Session(http)), new SessionTests.Navigation());
        await vm.CarregarAsync();
        Assert.True(vm.IsEmpty);
        Assert.False(vm.PodeMarcarTodas);
    }

    [Fact]
    public async Task FalhaAoMarcarNaoMarcaOtimisticamente()
    {
        using var http = Client(req => Task.FromResult(req.Method == HttpMethod.Post ? new(HttpStatusCode.InternalServerError) :
            req.RequestUri!.AbsolutePath.EndsWith("resumo") ? Json(new NotificacaoResumoDto(1)) : Json(new[] { new NotificacaoDto { Id = Guid.NewGuid() } })));
        var vm = new NotificacoesViewModel(new(Session(http)), new SessionTests.Navigation());
        await vm.CarregarAsync();
        await vm.MarcarAsync(vm.Itens[0]);
        Assert.False(vm.Itens[0].Lida);
        Assert.True(vm.HasError);
    }

    [Fact]
    public async Task EnvioEmAndamentoBloqueiaDuplicacao()
    {
        var pending = new TaskCompletionSource<HttpResponseMessage>();
        var posts = 0;
        using var http = Client(req =>
        {
            if (req.Method == HttpMethod.Get) return Task.FromResult(Json(Status()));
            posts++;
            return pending.Task;
        });
        var vm = new TutorIaViewModel(new(Session(http)), new SessionTests.Navigation()) { Mensagem = "Oi" };
        await vm.CarregarAsync();
        var sending = vm.EnviarAsync();
        Assert.True(vm.IsBusy);
        Assert.False(vm.PodeEnviar);
        await vm.EnviarAsync();
        Assert.Equal(1, posts);
        pending.SetResult(Json(Status(answer: "Resposta")));
        await sending;
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task HomeAbreNovasRotas()
    {
        var nav = new SessionTests.Navigation();
        var home = new HomeViewModel(Session(new HttpClient()), nav);
        await home.AbrirTutorIaAsync();
        Assert.Equal("TutorIA", nav.Page);
        await home.AbrirNotificacoesAsync();
        Assert.Equal("Notificacoes", nav.Page);
        await home.AbrirMinhasAulasAsync();
        Assert.Equal("MinhasAulas", nav.Page);
    }

    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var response = await handler(request);
            if (request.RequestUri!.AbsolutePath == "/api/tutoria/chat/stream" && response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(ct);
                response.Content.Dispose();
                response.Content = new StringContent($"event: complete\ndata: {json}\n\n", System.Text.Encoding.UTF8, "text/event-stream");
            }
            return response;
        }
    }
}

public sealed class ComunicacaoRenderTests
{
    [RenderFact]
    public async Task StatusChatENotificacoesReais()
    {
        using var http = new HttpClient { BaseAddress = new("https://tutorclass.onrender.com/"), Timeout = TimeSpan.FromSeconds(60) };
        var session = new SessionService(http, new SessionTests.Store());
        await session.LoginAsync(Environment.GetEnvironmentVariable("TUTORCLASS_TEST_EMAIL")!, Environment.GetEnvironmentVariable("TUTORCLASS_TEST_PASSWORD")!);
        var ia = new TutorIaService(session);
        var status = await ia.StatusAsync();
        Assert.False(string.IsNullOrWhiteSpace(status.Plano));
        var notices = new NotificacaoService(session);
        Assert.NotNull(await notices.ListarAsync(1));
        Assert.True((await notices.ResumoAsync()).NaoLidas >= 0);
        // Explicit opt-in, since a successful chat consumes one real usage.
        if (Environment.GetEnvironmentVariable("TUTORCLASS_TEST_CHAT") == "1")
        {
            Assert.True(status.PossuiAcesso, status.Mensagem);
            var response = await ia.EnviarAsync("Responda apenas: TutorClass Mobile funcionando.");
            Assert.False(string.IsNullOrWhiteSpace(response.Resposta), response.Mensagem);
        }
        session.Logout();
    }
}

