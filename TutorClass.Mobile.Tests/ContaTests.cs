using System.Net;
using System.Net.Http.Json;
using TutorClass.Mobile.Models;
using TutorClass.Mobile.Services;
using TutorClass.Mobile.ViewModels;

namespace TutorClass.Mobile.Tests;

public sealed class ContaTests
{
    private static readonly UsuarioAutenticadoDto User = new(Guid.NewGuid(), "Mariana", "mariana@example.com", "Aluno");
    private static HttpResponseMessage Json<T>(T value) => new(HttpStatusCode.OK) { Content = JsonContent.Create(value) };
    private static HttpClient Client(Func<HttpRequestMessage, HttpResponseMessage> handler) => new(new Handler(handler)) { BaseAddress = new("https://tutorclass.onrender.com/") };
    private static SessionService Session(HttpClient http, SessionTests.Store? store = null) => new(http, store ?? new() { Token = "token" });

    [Fact]
    public async Task PerfilUsaMeComJwtEDtoExistente()
    {
        using var http = Client(req =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Equal("/api/auth/me", req.RequestUri!.AbsolutePath);
            Assert.Equal("Bearer", req.Headers.Authorization?.Scheme);
            Assert.Equal("token", req.Headers.Authorization?.Parameter);
            return Json(User);
        });
        var vm = new PerfilViewModel(Session(http), new SessionTests.Navigation());
        await vm.CarregarAsync();
        Assert.Equal(User, vm.Usuario);
        Assert.True(vm.TemUsuario);
    }

    [Fact]
    public async Task LogoutLimpaTokenUsuarioENaoRestauraSessao()
    {
        var store = new SessionTests.Store { Token = "token" };
        using var http = Client(_ => Json(User));
        var session = Session(http, store);
        await session.RestoreAsync();
        var nav = new SessionTests.Navigation();
        nav.ShowHome();
        var vm = new PerfilViewModel(session, nav);
        await vm.CarregarAsync();
        vm.Logout();
        Assert.Null(store.Token);
        Assert.Null(session.Usuario);
        Assert.Null(vm.Usuario);
        Assert.Equal("Login", nav.Page);
        Assert.False(await session.RestoreAsync());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AssinaturaEBeneficiosSaoOsDaApi(bool ativa)
    {
        using var http = Client(req =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Equal("/api/planos/me", req.RequestUri!.AbsolutePath);
            Assert.Equal("token", req.Headers.Authorization?.Parameter);
            // Deliberately arbitrary label: there must be no client plan table.
            return Json(new PlanoStatusDto { PlanoAtual = "Plano retornado pela API", AssinaturaAtiva = ativa, PossuiIaEducacional = false, PossuiMentoria = true, PossuiGravacoesElegiveis = true, LimiteMensagensIaDia = 12 });
        });
        var vm = new PlanoViewModel(new(Session(http)), new SessionTests.Navigation());
        await vm.CarregarAsync();
        Assert.Equal("Plano retornado pela API", vm.Nome);
        Assert.Equal(ativa ? "Assinatura ativa" : "Sem assinatura ativa", vm.Assinatura);
        Assert.Contains("TutorIA: não disponível", vm.Beneficios);
        Assert.Contains("Mentoria: disponível", vm.Beneficios);
        Assert.Contains("12", vm.Beneficios);
        Assert.DoesNotContain("R$", vm.Beneficios);
    }

    [Fact]
    public async Task DadosAusentesNaoGeramPrecoOuBeneficios()
    {
        using var http = Client(_ => Json(new PlanoStatusDto { PlanoAtual = "Gratuito" }));
        var vm = new PlanoViewModel(new(Session(http)), new SessionTests.Navigation());
        await vm.CarregarAsync();
        Assert.Empty(vm.Beneficios);
        Assert.Equal("Status da assinatura não informado", vm.Assinatura);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SessaoExpiradaLimpaTokenEVoltaLogin(bool perfil)
    {
        var store = new SessionTests.Store { Token = "expired" };
        var nav = new SessionTests.Navigation();
        nav.ShowHome();
        using var http = Client(_ => new(HttpStatusCode.Unauthorized));
        var session = Session(http, store);
        if (perfil) await new PerfilViewModel(session, nav).CarregarAsync();
        else await new PlanoViewModel(new(session), nav).CarregarAsync();
        Assert.Null(store.Token);
        Assert.Equal("Login", nav.Page);
    }

    [Theory]
    [InlineData(403)]
    [InlineData(500)]
    public async Task ErroPlanoNaoInventaStatusEPermiteRecarga(int code)
    {
        int calls = 0;
        using var http = Client(_ => ++calls == 1 ? new((HttpStatusCode)code) : Json(new PlanoStatusDto { PlanoAtual = "Foco", AssinaturaAtiva = true }));
        var vm = new PlanoViewModel(new(Session(http)), new SessionTests.Navigation());
        await vm.CarregarAsync();
        Assert.True(vm.HasError);
        Assert.False(vm.TemPlano);
        await vm.CarregarAsync();
        Assert.False(vm.HasError);
        Assert.True(vm.TemPlano);
    }

    [Fact]
    public async Task PerfilNaoMantemDadosAposFalhaDeRecarga()
    {
        int calls = 0;
        using var http = Client(_ => ++calls == 1 ? Json(User) : throw new HttpRequestException());
        var vm = new PerfilViewModel(Session(http), new SessionTests.Navigation());
        await vm.CarregarAsync();
        await vm.CarregarAsync();
        Assert.Null(vm.Usuario);
        Assert.True(vm.HasError);
    }

    [Fact]
    public async Task NovasRotasPreservamHome()
    {
        var nav = new SessionTests.Navigation();
        var home = new HomeViewModel(Session(new HttpClient()), nav);
        await home.AbrirPerfilAsync();
        Assert.Equal("Perfil", nav.Page);
        await home.AbrirPlanoAsync();
        Assert.Equal("Plano", nav.Page);
        await home.AbrirProfessoresAsync();
        Assert.Equal("Professores", nav.Page);
    }

    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct) => Task.FromResult(handler(req));
    }
}

public sealed class ContaRenderTests
{
    [RenderFact]
    public async Task PerfilPlanoELogoutReais()
    {
        using var http = new HttpClient { BaseAddress = new("https://tutorclass.onrender.com/"), Timeout = TimeSpan.FromSeconds(60) };
        var store = new SessionTests.Store();
        var session = new SessionService(http, store);
        await session.LoginAsync(Environment.GetEnvironmentVariable("TUTORCLASS_TEST_EMAIL")!, Environment.GetEnvironmentVariable("TUTORCLASS_TEST_PASSWORD")!);
        var nav = new SessionTests.Navigation();
        var perfil = new PerfilViewModel(session, nav);
        await perfil.CarregarAsync();
        Assert.NotNull(perfil.Usuario);
        Assert.Equal("Aluno", perfil.Usuario.Perfil);
        Assert.False(string.IsNullOrWhiteSpace(perfil.Usuario.Nome));
        Assert.False(string.IsNullOrWhiteSpace(perfil.Usuario.Email));
        var plano = new PlanoViewModel(new(session), nav);
        await plano.CarregarAsync();
        Assert.True(plano.TemPlano, plano.Erro);
        Assert.False(string.IsNullOrWhiteSpace(plano.Nome));
        Assert.NotNull(plano.Plano!.AssinaturaAtiva);
        perfil.Logout();
        Assert.Null(store.Token);
        Assert.Null(session.Usuario);
        Assert.Equal("Login", nav.Page);
        Assert.False(await session.RestoreAsync());
    }
}
