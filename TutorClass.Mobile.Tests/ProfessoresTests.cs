using System.Net;
using TutorClass.Mobile.Services;
using TutorClass.Mobile.ViewModels;

namespace TutorClass.Mobile.Tests;

public sealed class ProfessoresTests
{
    private const string Id = "60d228cf-b310-4d6a-8b38-63775829f28b";
    private const string ProfessorJson = """
        {"id":"60d228cf-b310-4d6a-8b38-63775829f28b","nome":"Ana","materias":["Java","SQL"],
         "precoHora":85.5,"mediaAvaliacoes":4.8,"quantidadeAvaliacoes":2,"nivelReputacao":"Experiente",
         "qualificacoes":[{"titulo":"Mestrado","instituicao":"Universidade","dataConclusao":"2025-12-10"}],
         "disponibilidades":[{"diaSemana":"Monday","inicio":"09:00:00","fim":"10:00:00"}]}
        """;
    private static ProfessorService Service(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        => new(new HttpClient(new Handler(handler)) { BaseAddress = new("https://tutorclass.onrender.com/") });
    private static HttpResponseMessage Json(string json, HttpStatusCode code = HttpStatusCode.OK) => new(code) { Content = new StringContent(json) };

    [Fact]
    public async Task ListaMapeiaDadosENavegaPeloId()
    {
        var nav = new SessionTests.Navigation();
        var service = Service((request, _) =>
        {
            Assert.Equal("/api/professores", request.RequestUri!.AbsolutePath);
            Assert.Null(request.Headers.Authorization);
            return Task.FromResult(Json("[" + ProfessorJson + "]"));
        });
        var vm = new ProfessoresViewModel(service, nav);
        await new HomeViewModel(new SessionService(new HttpClient(), new SessionTests.Store()), nav).AbrirProfessoresAsync();
        Assert.Equal("Professores", nav.Page);
        await vm.CarregarAsync();
        var item = Assert.Single(vm.Professores);
        Assert.Equal("Ana", item.Nome);
        Assert.Equal("Java, SQL", item.Materias);
        Assert.Contains("85,50", item.Preco);
        Assert.Contains("4,8 / 5", item.Avaliacao);
        Assert.False(vm.IsEmpty);
        await vm.SelecionarAsync(item);
        Assert.Equal($"Professor/{Id}", nav.Page);
    }

    [Fact]
    public async Task DetalhesLeQualificacoesEDisponibilidades()
    {
        var vm = new ProfessorDetalhesViewModel(Service((request, _) =>
        {
            Assert.Equal("/api/professores/" + Id, request.RequestUri!.AbsolutePath);
            return Task.FromResult(Json(ProfessorJson));
        }), Guid.Parse(Id));
        await vm.CarregarAsync();
        Assert.True(vm.HasProfessor);
        Assert.Equal("Experiente", vm.Professor!.Reputacao);
        Assert.Contains("Mestrado", vm.Qualificacoes);
        Assert.Contains("10/12/2025", vm.Qualificacoes);
        Assert.Contains("segunda-feira: 09:00–10:00", vm.Disponibilidades);
    }

    [Fact]
    public async Task ListaVaziaTemEstadoProprio()
    {
        var vm = new ProfessoresViewModel(Service((_, _) => Task.FromResult(Json("[]"))), new SessionTests.Navigation());
        await vm.CarregarAsync();
        Assert.True(vm.IsEmpty);
        Assert.False(vm.HasError);
    }

    [Fact]
    public async Task Detalhes404NaoExibeDadosAntigos()
    {
        var count = 0;
        var vm = new ProfessorDetalhesViewModel(Service((_, _) => Task.FromResult(++count == 1 ? Json(ProfessorJson) : new(HttpStatusCode.NotFound))), Guid.Parse(Id));
        await vm.CarregarAsync();
        await vm.CarregarAsync();
        Assert.True(vm.NotFound);
        Assert.False(vm.HasProfessor);
        Assert.False(vm.HasError);
    }

    [Theory]
    [InlineData(500)]
    [InlineData(503)]
    [InlineData(403)]
    public async Task ErroApiControladoPodeSerRecarregado(int status)
    {
        var count = 0;
        var vm = new ProfessoresViewModel(Service((_, _) => Task.FromResult(++count == 1 ? Json("raw private error", (HttpStatusCode)status) : Json("[]"))), new SessionTests.Navigation());
        await vm.CarregarAsync();
        Assert.True(vm.HasError);
        Assert.False(vm.IsEmpty);
        Assert.DoesNotContain("raw", vm.Erro);
        await vm.CarregarAsync();
        Assert.False(vm.HasError);
        Assert.True(vm.IsEmpty);
    }

    [Fact]
    public async Task CamposOpcionaisAusentesNaoInventamValores()
    {
        var vm = new ProfessorDetalhesViewModel(Service((_, _) => Task.FromResult(Json($$"""{"id":"{{Id}}","nome":"Ana"}"""))), Guid.Parse(Id));
        await vm.CarregarAsync();
        Assert.Equal("Preço não informado", vm.Professor!.Preco);
        Assert.Equal("Sem avaliações", vm.Professor.Avaliacao);
        Assert.Equal("Matérias não informadas", vm.Professor.Materias);
        Assert.Equal("Qualificações não informadas", vm.Qualificacoes);
        Assert.Equal("Disponibilidades não informadas", vm.Disponibilidades);
    }

    [Fact]
    public async Task CarregamentoBloqueiaReentrada()
    {
        var response = new TaskCompletionSource<HttpResponseMessage>();
        var calls = 0;
        var vm = new ProfessoresViewModel(Service((_, _) => { calls++; return response.Task; }), new SessionTests.Navigation());
        var loading = vm.CarregarAsync();
        Assert.True(vm.IsBusy);
        Assert.False(vm.CanLoad);
        await vm.CarregarAsync();
        Assert.Equal(1, calls);
        response.SetResult(Json("[]"));
        await loading;
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task CancelamentoDaTelaNaoExibeErro()
    {
        using var cts = new CancellationTokenSource();
        var vm = new ProfessoresViewModel(Service(async (_, ct) => { await Task.Delay(Timeout.Infinite, ct); return Json("[]"); }), new SessionTests.Navigation());
        var loading = vm.CarregarAsync(cts.Token);
        cts.Cancel();
        await loading;
        Assert.False(vm.IsBusy);
        Assert.False(vm.HasError);
    }

    [Fact]
    public async Task TimeoutEJsonInvalidoSaoControlados()
    {
        var vm = new ProfessoresViewModel(Service((_, _) => throw new TaskCanceledException()), new SessionTests.Navigation());
        await vm.CarregarAsync();
        Assert.Contains("demorou", vm.Erro);
        vm = new ProfessoresViewModel(Service((_, _) => Task.FromResult(Json("invalid"))), new SessionTests.Navigation());
        await vm.CarregarAsync();
        Assert.True(vm.HasError);
    }

    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => response(request, cancellationToken);
    }
}

public sealed class ProfessoresRenderTests
{
    [ProfessoresRenderFact]
    public async Task ListaEDetalhesReais()
    {
        using var http = new HttpClient { BaseAddress = new("https://tutorclass.onrender.com/"), Timeout = TimeSpan.FromSeconds(60) };
        var service = new ProfessorService(http);
        var professores = await service.ListarAsync();
        Assert.NotEmpty(professores);
        var professor = await service.BuscarAsync(professores[0].Id);
        Assert.NotNull(professor);
        Assert.Equal(professores[0].Id, professor.Id);
        Assert.False(string.IsNullOrWhiteSpace(professor.Nome));
        Assert.Null(await service.BuscarAsync(Guid.Empty));
    }
}

public sealed class ProfessoresRenderFactAttribute : FactAttribute
{
    public ProfessoresRenderFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("TUTORCLASS_TEST_PUBLIC_API") != "1")
            Skip = "Opt-in: set TUTORCLASS_TEST_PUBLIC_API=1.";
    }
}
