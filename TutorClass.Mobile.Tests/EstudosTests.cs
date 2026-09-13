using System.Net;
using TutorClass.Mobile.Services;
using TutorClass.Mobile.ViewModels;

namespace TutorClass.Mobile.Tests;

public sealed class EstudosTests
{
    private static HttpClient Client(Func<HttpRequestMessage, HttpResponseMessage> handler) => new(new Handler(handler)) { BaseAddress = new("https://tutorclass.onrender.com/") };
    private static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK) { Content = new StringContent(json) };

    [Fact]
    public async Task MateriasLeSomenteContratoPublico()
    {
        var vm = new MateriasViewModel(new MateriaService(Client(req =>
        {
            Assert.Equal("/api/materias", req.RequestUri!.AbsolutePath);
            Assert.Null(req.Headers.Authorization);
            return Json("""[{"id":"6332ff9d-dc4c-4949-ae0b-d82a10dba9fd","nome":"Java"}]""");
        })));
        await vm.CarregarAsync();
        Assert.Equal("Java", Assert.Single(vm.Itens).Nome);
        Assert.False(vm.IsEmpty);
    }

    [Fact]
    public async Task AulasUsaJwtSemIdDeAlunoNaRequisicao()
    {
        var store = new SessionTests.Store { Token = "student-token" };
        var session = new SessionService(Client(req =>
        {
            Assert.Equal("/api/aulas/minhas", req.RequestUri!.AbsolutePath);
            Assert.Empty(req.RequestUri.Query);
            Assert.Equal("Bearer", req.Headers.Authorization?.Scheme);
            Assert.Equal("student-token", req.Headers.Authorization?.Parameter);
            return Json("""[{"id":"6332ff9d-dc4c-4949-ae0b-d82a10dba9fd","materia":"Java","professor":"Ana","inicio":"2026-09-12T19:00:00Z","duracaoMinutos":60,"status":"Confirmada","precoAcordado":90}]""");
        }), store);
        var vm = new MinhasAulasViewModel(new AulaService(session), new SessionTests.Navigation());
        await vm.CarregarAsync();
        var aula = Assert.Single(vm.Itens);
        Assert.Equal("Java", aula.Materia);
        Assert.Equal("Ana", aula.Professor);
        Assert.Equal("Confirmada", aula.Status);
        Assert.Contains("90,00", aula.Valor);
        Assert.Contains("horário do dispositivo", aula.Horario);
        Assert.Equal("60 minutos", aula.Duracao);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ListasVaziasSemErro(bool materias)
    {
        using var http = Client(_ => Json("[]"));
        if (materias)
        {
            var vm = new MateriasViewModel(new MateriaService(http));
            await vm.CarregarAsync();
            Assert.True(vm.IsEmpty);
            Assert.False(vm.HasError);
        }
        else
        {
            var vm = Aulas(http);
            await vm.CarregarAsync();
            Assert.True(vm.IsEmpty);
            Assert.False(vm.HasError);
        }
    }

    [Theory]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(500)]
    public async Task AulasTrataFalhasSemVazarErroBruto(int status)
    {
        var store = new SessionTests.Store { Token = "token" };
        var nav = new SessionTests.Navigation();
        nav.ShowHome();
        var vm = new MinhasAulasViewModel(new AulaService(new SessionService(Client(_ => new((HttpStatusCode)status)
            { Content = new StringContent("private error") }), store)), nav);
        await vm.CarregarAsync();
        Assert.Empty(vm.Itens);
        Assert.True(vm.HasError);
        Assert.DoesNotContain("private", vm.Erro);
        Assert.Equal(status == 401 ? null : "token", store.Token);
        Assert.Equal(status == 401 ? "Login" : "Home", nav.Page);
    }

    [Fact]
    public async Task TokenAusenteNaoChamaApi()
    {
        var vm = new MinhasAulasViewModel(new AulaService(new SessionService(Client(_ => throw new Exception("unexpected")), new SessionTests.Store())), new SessionTests.Navigation());
        await vm.CarregarAsync();
        Assert.Contains("sessão expirou", vm.Erro);
    }

    [Fact]
    public async Task MateriasErroRecargaEJsonInvalido()
    {
        var count = 0;
        var vm = new MateriasViewModel(new MateriaService(Client(_ => ++count == 1 ? Json("invalid") : Json("[]"))));
        await vm.CarregarAsync();
        Assert.True(vm.HasError);
        Assert.Contains("matérias", vm.Erro);
        await vm.CarregarAsync();
        Assert.True(vm.IsEmpty);
        Assert.False(vm.HasError);
    }

    [Fact]
    public async Task AulasErroLimpaDadosAnteriores()
    {
        var count = 0;
        var vm = Aulas(Client(_ => ++count == 1 ? Json("""[{"materia":"Java"}]""") : new(HttpStatusCode.InternalServerError)));
        await vm.CarregarAsync();
        Assert.Single(vm.Itens);
        await vm.CarregarAsync();
        Assert.Empty(vm.Itens);
        Assert.True(vm.HasError);
        Assert.False(vm.IsEmpty);
    }

    [Fact]
    public async Task HomePreservaRotasEAbreNovasTelas()
    {
        var nav = new SessionTests.Navigation();
        var vm = new HomeViewModel(new SessionService(new HttpClient(), new SessionTests.Store()), nav);
        await vm.AbrirMateriasAsync();
        Assert.Equal("Materias", nav.Page);
        await vm.AbrirMinhasAulasAsync();
        Assert.Equal("MinhasAulas", nav.Page);
        await vm.AbrirProfessoresAsync();
        Assert.Equal("Professores", nav.Page);
    }

    [Fact]
    public void AulasNaoInventaValorOuData()
    {
        var item = new AulaItemViewModel(new());
        Assert.Equal("Valor não informado", item.Valor);
        Assert.Equal("Horário não informado", item.Horario);
    }

    private static MinhasAulasViewModel Aulas(HttpClient http) => new(new AulaService(new SessionService(http, new SessionTests.Store { Token = "token" })), new SessionTests.Navigation());
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => Task.FromResult(response(request));
    }
}

public sealed class EstudosRenderTests
{
    [ProfessoresRenderFact]
    public async Task MateriasReais()
    {
        using var http = new HttpClient { BaseAddress = new("https://tutorclass.onrender.com/"), Timeout = TimeSpan.FromSeconds(60) };
        var materias = await new MateriaService(http).ListarAsync();
        Assert.NotEmpty(materias);
        Assert.All(materias, m => Assert.False(string.IsNullOrWhiteSpace(m.Nome)));
    }

    [RenderFact]
    public async Task AulasReaisAutenticadas()
    {
        using var http = new HttpClient { BaseAddress = new("https://tutorclass.onrender.com/"), Timeout = TimeSpan.FromSeconds(60) };
        var session = new SessionService(http, new SessionTests.Store());
        await session.LoginAsync(Environment.GetEnvironmentVariable("TUTORCLASS_TEST_EMAIL")!, Environment.GetEnvironmentVariable("TUTORCLASS_TEST_PASSWORD")!);
        Assert.Equal("Aluno", session.Usuario!.Perfil);
        var aulas = await new AulaService(session).ListarMinhasAsync();
        Assert.NotNull(aulas);
        Assert.All(aulas, a => { Assert.False(string.IsNullOrWhiteSpace(a.Materia)); Assert.False(string.IsNullOrWhiteSpace(a.Professor)); });
        session.Logout();
    }
}


