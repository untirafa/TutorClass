using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TutorClass.API.Middleware;
using TutorClass.Application.Abstractions;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;
using TutorClass.Domain.Plans;
using TutorClass.Domain.Users;
using TutorClass.Infrastructure.Persistence;

namespace TutorClass.Tests;

internal sealed class PerformanceLog<T> : ILogger<T>
{
    public List<Dictionary<string, object?>> Entries { get; } = [];
    public string Text => string.Join("|", Entries.SelectMany(x => x.Values));
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel level) => true;
    public void Log<TState>(LogLevel level, EventId id, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
        => Entries.Add(((IEnumerable<KeyValuePair<string, object?>>)state!).ToDictionary(x => x.Key, x => x.Value));
}

public class TutorIaPerformanceTests
{
    [Theory]
    [InlineData("Explique fotossíntese", false)]
    [InlineData("vou te bater", true)]
    public async Task Mede_etapas_sem_identidade_conteudo_ou_mudanca_de_cota(string input, bool blocked)
    {
        await using var db = new TutorClassDbContext(new DbContextOptionsBuilder<TutorClassDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var aluno = new Aluno("Nome privado", "privado@example.test", "hash");
        db.Add(aluno);
        db.Add(new AssinaturaAluno(aluno.Id, aluno.Id, PlanoAluno.Foco, 30, 20, true));
        await db.SaveChangesAsync();
        var log = new PerformanceLog<TutorIaService>();
        var provider = new Provider();
        var result = await new TutorIaService(db, provider, logger: log)
            .ConversarAsync(aluno.Id, new(input), default);
        Assert.Equal(blocked ? 0 : 1, provider.Calls);
        Assert.Equal(blocked ? 0 : 1, result.UsadasHoje);
        var stages = log.Entries.Select(x => x["Stage"]).ToArray();
        foreach (var stage in new[] { "EsperaTrava", "PlanoCota", "Moderacao", "ProcessamentoTotal" })
            Assert.Contains(stage, stages);
        Assert.Equal(!blocked, stages.Contains("ConsultaContexto"));
        Assert.Equal(!blocked, stages.Contains("MontagemContexto"));
        Assert.All(log.Entries, x => Assert.True((double)x["DurationMs"]! >= 0));
        foreach (var secret in new[] { input, aluno.Id.ToString(), "Nome privado", "privado@example.test", "Resposta privada" })
            Assert.DoesNotContain(secret, log.Text);
    }

    [Theory]
    [InlineData("/api/tutoria/chat")]
    [InlineData("/api/tutoria/chat/stream")]
    public async Task Mede_entrada_e_conclusao_http_inclusive_cancelamento(string path)
    {
        foreach (var cancel in new[] { false, true })
        {
            var log = new PerformanceLog<TutorIaPerformanceMiddleware>();
            var context = new DefaultHttpContext();
            context.Request.Method = "POST";
            context.Request.Path = path;
            var middleware = new TutorIaPerformanceMiddleware(ctx =>
            {
                TutorIaPerformanceMiddleware.MarcarInicio(ctx);
                if (cancel) throw new OperationCanceledException();
                return Task.CompletedTask;
            }, log);
            if (cancel) await Assert.ThrowsAsync<OperationCanceledException>(() => middleware.InvokeAsync(context));
            else await middleware.InvokeAsync(context);
            Assert.Equal(new[] { "EntradaAteProcessamento", "RequisicaoTotal" }, log.Entries.Select(x => x["Stage"]));
            Assert.Empty(context.Items);
        }
    }

    private sealed class Provider : ITutorIaProvider
    {
        public int Calls;
        public Task<string> ResponderAsync(TutorIaProviderRequest request, CancellationToken ct)
        { Calls++; return Task.FromResult("Resposta privada"); }
    }
}
