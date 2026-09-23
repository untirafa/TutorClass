using System.Diagnostics;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;
using TutorClass.Domain.Plans;
using TutorClass.Domain.Users;
using TutorClass.Infrastructure.AI;
using TutorClass.Infrastructure.Persistence;

namespace TutorClass.Tests;

public sealed class GeminiResilienciaTests
{
    [Theory]
    [InlineData(503, 200, 2)]
    [InlineData(429, 200, 2)]
    [InlineData(503, 503, 3)]
    public async Task Retry_com_sucesso_final_consume_exatamente_um_uso(int primeiro, int segundo, int chamadas)
    {
        await using var db = Db(); var aluno = Seed(db);
        var handler = new SequenceHandler(primeiro, segundo, 200);
        using var http = new HttpClient(handler);
        var result = await new TutorIaService(db, Provider(http)).ConversarAsync(aluno.Id, new("Explique classes"), default);
        Assert.Equal("Resposta Gemini", result.Resposta);
        Assert.Equal(chamadas, handler.Chamadas);
        Assert.Equal(1, db.UsosIaAluno.Single().Quantidade);
        Assert.Equal(6, result.RestantesHoje);
        Assert.All(handler.Bodies, body => Assert.Equal(handler.Bodies[0], body));
        Assert.True(handler.Times[1] - handler.Times[0] >= TimeSpan.FromMilliseconds(900));
        if (chamadas == 3) Assert.True(handler.Times[2] - handler.Times[1] >= TimeSpan.FromMilliseconds(1900));
    }

    [Theory]
    [InlineData(false, 503)]
    [InlineData(false, 429)]
    [InlineData(true, 503)]
    [InlineData(true, 429)]
    public async Task Tres_falhas_respeitam_fallback_e_quota(bool fallback, int status)
    {
        await using var db = Db(); var aluno = Seed(db);
        var handler = new SequenceHandler(status, status, status);
        using var http = new HttpClient(handler);
        var log = new CaptureLogger();
        var result = await new TutorIaService(db, Provider(http, fallback, log)).ConversarAsync(aluno.Id, new("Explique classes"), default);
        Assert.Equal(3, handler.Chamadas);
        if (fallback)
        {
            Assert.StartsWith("TutorIA simulada:", result.Resposta);
            Assert.Equal(1, db.UsosIaAluno.Single().Quantidade);
            Assert.Contains(log.Messages, m => m.Contains("FallbackApresentacao=True") && m.Contains("ToProvider=Simulado"));
        }
        else
        {
            Assert.Equal("ERRO_TUTORIA", result.Motivo);
            Assert.Equal("A TutorIA está temporariamente indisponível. Tente novamente em alguns instantes.", result.Mensagem);
            Assert.Empty(db.UsosIaAluno);
            Assert.DoesNotContain(log.Messages, m => m.Contains("FallbackApresentacao=True"));
        }
    }

    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(500)]
    [InlineData(0)]
    public async Task Outras_falhas_nao_repetem_nem_acionam_contingencia(int status)
    {
        var handler = new SequenceHandler(status);
        using var actualHttp = new HttpClient(handler);
        await Assert.ThrowsAsync<TutorIaProviderException>(() => Provider(actualHttp, true).ResponderAsync(Request(), default));
        Assert.Equal(1, handler.Chamadas);
    }

    [Fact]
    public async Task Cancelamento_durante_espera_nao_repete_nao_faz_fallback_nem_consume()
    {
        await using var db = Db(); var aluno = Seed(db);
        using var cts = new CancellationTokenSource();
        var handler = new SequenceHandler(503, 200);
        using var http = new HttpClient(handler);
        var task = new TutorIaService(db, Provider(http, true)).ConversarAsync(aluno.Id, new("oi"), cts.Token);
        await handler.FirstCall.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.Equal(1, handler.Chamadas);
        Assert.Empty(db.UsosIaAluno);
    }

    [Fact]
    public async Task Validacao_sem_contexto_nao_chama_http_ou_fallback()
    {
        var handler = new SequenceHandler(200);
        using var http = new HttpClient(handler);
        await Assert.ThrowsAsync<TutorIaProviderException>(() => Provider(http, true).ResponderAsync(Request() with { Contexto = null }, default));
        Assert.Equal(0, handler.Chamadas);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("false", false)]
    [InlineData("true", true)]
    public void Configuracao_TutorIA_lida_e_desligada_por_padrao(string? value, bool expected)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["TutorIA:Provider"] = "Gemini", ["Gemini:ApiKey"] = "fake", ["Gemini:Model"] = "fake",
            ["TutorIA:FallbackApresentacao"] = value
        }).Build();
        using var services = new ServiceCollection().AddTutorIaProvider(config).BuildServiceProvider();
        Assert.Equal(expected, services.GetRequiredService<TutorIaContingenciaOptions>().FallbackApresentacao);
    }

    private static GeminiTutorIaProvider Provider(HttpClient http, bool fallback = false, CaptureLogger? log = null) =>
        new(http, new() { ApiKey = "fake", Model = "fake", FallbackModel = "" }, log ?? new(), new() { FallbackApresentacao = fallback }, new TutorIaSimuladaProvider());
    private static TutorIaProviderRequest Request() => new(Guid.NewGuid(), PlanoAluno.Foco, "oi", default, new());
    private static TutorClassDbContext Db() => new(new DbContextOptionsBuilder<TutorClassDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static Aluno Seed(TutorClassDbContext db)
    {
        var aluno = new Aluno("Aluno", Guid.NewGuid() + "@test.local", "hash");
        db.Add(aluno); db.Add(new AssinaturaAluno(aluno.Id, aluno.Id, PlanoAluno.Foco, 30, 20, true)); db.SaveChanges(); return aluno;
    }
    private sealed class SequenceHandler(params int[] statuses) : HttpMessageHandler
    {
        public int Chamadas;
        public List<string> Bodies = [];
        public List<TimeSpan> Times = [];
        private readonly Stopwatch clock = Stopwatch.StartNew();
        public TaskCompletionSource FirstCall = new(TaskCreationOptions.RunContinuationsAsynchronously);
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Bodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken)); Times.Add(clock.Elapsed);
            var status = statuses[Chamadas++]; FirstCall.TrySetResult();
            if (status == 0) throw new TaskCanceledException();
            return new((HttpStatusCode)status) { Content = new StringContent(status == 200
                ? """{"candidates":[{"content":{"parts":[{"text":"Resposta Gemini"}]}}]}""" : "{}") };
        }
    }
    private sealed class CaptureLogger : ILogger<GeminiTutorIaProvider>
    {
        public List<string> Messages = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => true;
        public void Log<TState>(LogLevel level, EventId id, TState state, Exception? error, Func<TState, Exception?, string> formatter) => Messages.Add(formatter(state, error));
    }
}
