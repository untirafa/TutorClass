using System.Net;
using System.Net.Http.Json;
using TutorClass.Mobile.Models;
using TutorClass.Mobile.Services;
using TutorClass.Mobile.ViewModels;

namespace TutorClass.Mobile.Tests;

public sealed class SessionTests
{
    private static readonly UsuarioAutenticadoDto User = new(Guid.NewGuid(), "Aluno Teste", "aluno@example.com", "Aluno");
    private static HttpResponseMessage Json<T>(T value) => new(HttpStatusCode.OK) { Content = JsonContent.Create(value) };

    [Fact]
    public async Task LoginConfirmaBearerSalvaTokenENavegaParaHome()
    {
        var calls = 0;
        var handler = new Handler(request =>
        {
            calls++;
            if (calls == 1)
            {
                Assert.Equal("/api/auth/login", request.RequestUri!.AbsolutePath);
                Assert.Null(request.Headers.Authorization);
                return Json(new LoginResponse("test-token", DateTime.UtcNow.AddHours(1), User));
            }
            Assert.Equal("/api/auth/me", request.RequestUri!.AbsolutePath);
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            Assert.Equal("test-token", request.Headers.Authorization?.Parameter);
            return Json(User);
        });
        var store = new Store();
        var session = new SessionService(Client(handler), store);
        var nav = new Navigation();
        var vm = new LoginViewModel(session, nav) { Email = User.Email, Senha = "test-password" };
        await vm.LoginAsync();
        Assert.Equal(2, calls);
        Assert.Equal("test-token", store.Token);
        Assert.Equal("Home", nav.Page);
        Assert.Equal(User.Nome, new HomeViewModel(session, nav).Nome);
        Assert.Empty(vm.Senha);
        Assert.Empty(vm.Erro);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task LoginInvalidoNaoSalvaTokenNemNavega()
    {
        var store = new Store();
        var nav = new Navigation();
        var vm = new LoginViewModel(new SessionService(Client(new Handler(_ => new(HttpStatusCode.Unauthorized))), store), nav)
            { Email = User.Email, Senha = "wrong" };
        await vm.LoginAsync();
        Assert.Equal("Email ou senha inválidos.", vm.Erro);
        Assert.Null(store.Token);
        Assert.Equal("Login", nav.Page);
    }

    [Fact]
    public async Task CamposVaziosNaoChamamApi()
    {
        var vm = new LoginViewModel(new SessionService(Client(new Handler(_ => throw new Exception("Unexpected request"))), new Store()), new Navigation());
        await vm.LoginAsync();
        Assert.Equal("Informe seu email e sua senha.", vm.Erro);
    }

    [Fact]
    public async Task SessaoExpiradaLimpaToken()
    {
        var store = new Store { Token = "expired" };
        var session = new SessionService(Client(new Handler(_ => new(HttpStatusCode.Unauthorized))), store);
        Assert.False(await session.RestoreAsync());
        Assert.Null(store.Token);
        Assert.Null(session.Usuario);
    }

    [Fact]
    public async Task RestauraSessaoELogoutVoltaAoLogin()
    {
        var store = new Store { Token = "valid" };
        var session = new SessionService(Client(new Handler(_ => Json(User))), store);
        var nav = new Navigation();
        await new LoginViewModel(session, nav).RestoreAsync();
        Assert.Equal("Home", nav.Page);
        new HomeViewModel(session, nav).Logout();
        Assert.Equal("Login", nav.Page);
        Assert.Null(store.Token);
        Assert.Null(session.Usuario);
    }

    [Fact]
    public async Task FalhaDeRedePreservaTokenENaoAbreHome()
    {
        var store = new Store { Token = "valid" };
        var nav = new Navigation();
        var vm = new LoginViewModel(new SessionService(Client(new Handler(_ => throw new HttpRequestException())), store), nav);
        await vm.RestoreAsync();
        Assert.Equal("valid", store.Token);
        Assert.Equal("Login", nav.Page);
        Assert.Contains("conexão", vm.Erro);
    }

    [Fact]
    public async Task MeRejeitadoNaoPersisteNovoToken()
    {
        var store = new Store();
        var session = new SessionService(Client(new Handler(req => req.Method == HttpMethod.Post
            ? Json(new LoginResponse("rejected", DateTime.UtcNow.AddHours(1), User))
            : new(HttpStatusCode.Unauthorized))), store);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => session.LoginAsync(User.Email, "test"));
        Assert.Null(store.Token);
    }

    [Fact]
    public async Task RespostaInesperadaTemErroControlado()
    {
        var nav = new Navigation();
        var vm = new LoginViewModel(new SessionService(Client(new Handler(_ => new(HttpStatusCode.OK)
            { Content = new StringContent("invalid json") })), new Store()), nav) { Email = User.Email, Senha = "test" };
        await vm.LoginAsync();
        Assert.NotEmpty(vm.Erro);
        Assert.Equal("Login", nav.Page);
    }

    private static HttpClient Client(HttpMessageHandler handler) => new(handler) { BaseAddress = new("https://tutorclass.onrender.com/") };
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(response(request));
    }
    internal sealed class Store : ITokenStore
    {
        public string? Token { get; set; }
        public Task<string?> ReadAsync() => Task.FromResult(Token);
        public Task SaveAsync(string token) { Token = token; return Task.CompletedTask; }
        public void Clear() => Token = null;
    }
    internal sealed class Navigation : IAppNavigation
    {
        public string Page { get; private set; } = "Login";
        public void ShowHome() => Page = "Home";
        public void ShowLogin() => Page = "Login";
        public Task ShowTutorIaAsync() { Page = "TutorIA"; return Task.CompletedTask; }
        public Task ShowNotificacoesAsync() { Page = "Notificacoes"; return Task.CompletedTask; }
        public Task ShowPerfilAsync() { Page = "Perfil"; return Task.CompletedTask; }
        public Task ShowPlanoAsync() { Page = "Plano"; return Task.CompletedTask; }
        public Task ShowMateriasAsync() { Page = "Materias"; return Task.CompletedTask; }
        public Task ShowMinhasAulasAsync() { Page = "MinhasAulas"; return Task.CompletedTask; }
        public Task ShowProfessoresAsync() { Page = "Professores"; return Task.CompletedTask; }
        public Task ShowProfessorAsync(Guid id) { Page = $"Professor/{id}"; return Task.CompletedTask; }
    }
}




