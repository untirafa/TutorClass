using System.IO.Pipelines;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using TutorClass.Application.Abstractions;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;
using TutorClass.Domain.Plans;
using TutorClass.Domain.Users;
using TutorClass.Infrastructure.AI;
using TutorClass.Infrastructure.Persistence;

namespace TutorClass.Tests;

public sealed class TutorIaStreamingTests
{
    private static string Frame(string text, string? finish = null) => "data: " + System.Text.Json.JsonSerializer.Serialize(new
    {
        candidates = new[] { new { content = new { parts = new[] { new { text } } }, finishReason = finish ?? "STOP" } }
    }) + "\n\n";
    private const string Partial = "data: {\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"ação \"}]}}]}\n\n";

    [Fact]
    public async Task Gemini_entrega_primeiro_trecho_antes_do_fim_e_quota_so_apos_sucesso()
    {
        var pipe = new Pipe();
        using var http = new HttpClient(new Handler(() => new StreamContent(pipe.Reader.AsStream())));
        var provider = Provider(http);
        await using var db = Db(); var aluno = Seed(db);
        var memoria = new TutorIaMemoria();
        var first = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var text = "";
        var task = new TutorIaService(db, provider, memoria: memoria).ConversarAsync(aluno.Id, new("Explique frações"), (chunk, _) =>
        { text += chunk; first.TrySetResult(); return Task.CompletedTask; }, default);
        await pipe.Writer.WriteAsync(Encoding.UTF8.GetBytes(Partial));
        await first.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("ação ", text); Assert.False(task.IsCompleted);
        Assert.Empty(db.UsosIaAluno); Assert.Empty(memoria.Obter(aluno.Id));
        await pipe.Writer.WriteAsync(Encoding.UTF8.GetBytes(Frame("concluída")));
        await pipe.Writer.CompleteAsync();
        var result = await task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("ação concluída", result.Resposta);
        Assert.Equal(1, db.UsosIaAluno.Single().Quantidade);
        Assert.Equal(2, memoria.Obter(aluno.Id).Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("data: {\"candidates\":[{\"finishReason\":\"MAX_TOKENS\"}]}\n\n")]
    [InlineData("data: {\"candidates\":[{\"finishReason\":\"SAFETY\"}]}\n\n")]
    public async Task Falha_apos_trecho_nao_repete_nao_consume_nem_salva_memoria(string ending)
    {
        var handler = new Handler(() => new StringContent(Partial + ending));
        using var http = new HttpClient(handler);
        await using var db = Db(); var aluno = Seed(db); var memoria = new TutorIaMemoria();
        var chunks = new List<string>();
        var result = await new TutorIaService(db, Provider(http), memoria: memoria).ConversarAsync(aluno.Id, new("oi"),
            (chunk, _) => { chunks.Add(chunk); return Task.CompletedTask; }, default);
        Assert.Single(chunks); Assert.Equal("ERRO_TUTORIA", result.Motivo);
        Assert.Empty(db.UsosIaAluno); Assert.Empty(memoria.Obter(aluno.Id)); Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task Cancelamento_apos_trecho_nao_consume()
    {
        var pipe = new Pipe();
        using var http = new HttpClient(new Handler(() => new StreamContent(pipe.Reader.AsStream())));
        await using var db = Db(); var aluno = Seed(db);
        using var cts = new CancellationTokenSource();
        var first = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var task = new TutorIaService(db, Provider(http)).ConversarAsync(aluno.Id, new("oi"),
            (_, _) => { first.TrySetResult(); return Task.CompletedTask; }, cts.Token);
        await pipe.Writer.WriteAsync(Encoding.UTF8.GetBytes(Partial));
        await first.Task.WaitAsync(TimeSpan.FromSeconds(5)); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Empty(db.UsosIaAluno); await pipe.Writer.CompleteAsync();
    }

    [Fact]
    public async Task Pensamento_nao_e_exibido_e_timeout_cobre_corpo_do_stream()
    {
        var body = "data: {\"candidates\":[{\"content\":{\"parts\":[{\"thought\":true,\"text\":\"interno\"}]}}]}\n\n" + Frame("Visível");
        using var http = new HttpClient(new Handler(() => new StringContent(body)));
        var text = "";
        await Provider(http).ResponderProgressivamenteAsync(Request(), (chunk, _) => { text += chunk; return Task.CompletedTask; }, default);
        Assert.Equal("Visível", text);
        var pipe = new Pipe();
        using var stalled = new HttpClient(new Handler(() => new StreamContent(pipe.Reader.AsStream())));
        var timed = new GeminiTutorIaProvider(stalled, new() { ApiKey="fake", Model="fake", FallbackModel="", TimeoutSeconds=1 }, NullLogger<GeminiTutorIaProvider>.Instance);
        var error = await Assert.ThrowsAsync<TutorIaProviderException>(() => timed.ResponderProgressivamenteAsync(Request(), (_, _) => Task.CompletedTask, default));
        Assert.Equal("timeout", error.Categoria); await pipe.Writer.CompleteAsync();
    }

    [Fact]
    public async Task Endpoint_exige_autenticacao_e_flush_do_delta_antecede_complete()
    {
        var provider = new GateProvider();
        await using var factory = new TutorClassApiFactory(services =>
        { services.RemoveAll<ITutorIaProvider>(); services.AddSingleton<ITutorIaProvider>(provider); });
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/tutoria/chat/stream", new { mensagem="oi" })).StatusCode);
        await client.PostAsJsonAsync("/api/auth/registrar/aluno", new RegistrarAlunoRequest("Aluno", "stream@example.test", "Test-password-123!", null));
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("stream@example.test", "Test-password-123!"));
        var login = (await loginResponse.Content.ReadFromJsonAsync<LoginResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var blocked = await client.PostAsJsonAsync("/api/tutoria/chat/stream", new { mensagem="oi" });
        var blockedBody = await blocked.Content.ReadAsStringAsync();
        Assert.Contains("PLANO_SEM_TUTORIA", blockedBody); Assert.DoesNotContain("event: delta", blockedBody);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorClassDbContext>();
            db.Add(new AssinaturaAluno(login.Usuario.Id, login.Usuario.Id, PlanoAluno.Foco, 30, 20, true)); await db.SaveChangesAsync();
        }
        var invalid = await client.PostAsJsonAsync("/api/tutoria/chat/stream", new { mensagem=new string('x',4001) });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal("application/json", invalid.Content.Headers.ContentType!.MediaType);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/tutoria/chat/stream") { Content = JsonContent.Create(new { mensagem="oi" }) };
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType!.MediaType);
        using var reader = new StreamReader(await response.Content.ReadAsStreamAsync());
        Assert.Equal("event: delta", await reader.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Contains("primeiro", await reader.ReadLineAsync());
        Assert.False(provider.Finish.Task.IsCompleted);
        provider.Finish.SetResult();
        var tail = await reader.ReadToEndAsync().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Contains("event: complete", tail); Assert.Contains("\"usadasHoje\":1", tail); Assert.Contains("\"plano\":\"Foco\"", tail);
    }

    private static GeminiTutorIaProvider Provider(HttpClient client) => new(client, new() { ApiKey="fake", Model="fake", FallbackModel="" }, NullLogger<GeminiTutorIaProvider>.Instance);
    private static TutorIaProviderRequest Request() => new(Guid.NewGuid(), PlanoAluno.Foco, "oi", default, new());
    private static TutorClassDbContext Db() => new(new DbContextOptionsBuilder<TutorClassDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static Aluno Seed(TutorClassDbContext db)
    {
        var aluno = new Aluno("Aluno", Guid.NewGuid()+"@example.test", "hash");
        db.Add(aluno); db.Add(new AssinaturaAluno(aluno.Id, aluno.Id, PlanoAluno.Foco, 30, 20, true)); db.SaveChanges(); return aluno;
    }
    private sealed class Handler(Func<HttpContent> content) : HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++; Assert.EndsWith(":streamGenerateContent?alt=sse", request.RequestUri!.ToString());
            var body = content(); body.Headers.ContentType = new("text/event-stream");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content=body });
        }
    }
    private sealed class GateProvider : ITutorIaProvider
    {
        public TaskCompletionSource Finish { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<string> ResponderAsync(TutorIaProviderRequest request, CancellationToken ct) => throw new InvalidOperationException();
        public async Task<string> ResponderProgressivamenteAsync(TutorIaProviderRequest request, Func<string,CancellationToken,Task> callback, CancellationToken ct)
        { await callback("primeiro", ct); await Finish.Task.WaitAsync(ct); return "primeiro completo"; }
    }
}
