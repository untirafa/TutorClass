using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TutorClass.Application.DTOs;
using TutorClass.Domain.Plans;
using TutorClass.Infrastructure.AI;

namespace TutorClass.Tests;

public class GeminiModelFallbackTests
{
    [Theory]
    [InlineData(503)]
    [InlineData(429)]
    [InlineData(500)]
    public async Task Falha_transitoria_troca_modelo_e_entrega_texto(int status)
    {
        var handler = new Handler(status);
        using var http = new HttpClient(handler);
        var text = "";
        var result = await Provider(http).ResponderProgressivamenteAsync(Request(),
            (chunk, _) => { text += chunk; return Task.CompletedTask; }, default);
        Assert.Equal("Resposta real", result);
        Assert.Equal(result, text);
        Assert.Equal(2, handler.Paths.Count);
        Assert.Contains("primary:streamGenerateContent", handler.Paths[0]);
        Assert.Contains("gemini-3.1-flash-lite:streamGenerateContent", handler.Paths[1]);
    }

    [Fact]
    public async Task Modelo_sem_primeiro_texto_e_cancelado_e_substituido()
    {
        var handler = new Handler(0);
        using var http = new HttpClient(handler);
        var result = await Provider(http).ResponderProgressivamenteAsync(Request(), (_, _) => Task.CompletedTask, default);
        Assert.Equal("Resposta real", result);
        Assert.True(handler.Cancelled);
        Assert.Equal(2, handler.Paths.Count);
    }

    [Theory]
    [InlineData(401)]
    [InlineData(403)]
    public async Task Credencial_invalida_nao_troca_modelo(int status)
    {
        var handler = new Handler(status);
        using var http = new HttpClient(handler);
        await Assert.ThrowsAsync<TutorIaProviderException>(() => Provider(http)
            .ResponderProgressivamenteAsync(Request(), (_, _) => Task.CompletedTask, default));
        Assert.Single(handler.Paths);
    }

    [Fact]
    public async Task Texto_parcial_nao_mistura_respostas_de_modelos()
    {
        var handler = new Handler(200, partial: true);
        using var http = new HttpClient(handler);
        var text = "";
        await Assert.ThrowsAsync<TutorIaProviderException>(() => Provider(http).ResponderProgressivamenteAsync(Request(),
            (chunk, _) => { text += chunk; return Task.CompletedTask; }, default));
        Assert.Equal("Resposta real", text);
        Assert.Single(handler.Paths);
    }

    [Theory]
    [InlineData(0, "timeout")]
    [InlineData(503, "servidor_provider")]
    public async Task Medicoes_de_tentativas_e_primeiro_texto_sem_conteudo(int status, string category)
    {
        using var http = new HttpClient(new Handler(status));
        var log = new PerformanceLog<GeminiTutorIaProvider>();
        var provider = new GeminiTutorIaProvider(http,
            new() { ApiKey = "segredo-simulado", Model = "primary", FirstTextTimeoutSeconds = 1 }, log);
        await provider.ResponderProgressivamenteAsync(Request(), (_, _) => Task.CompletedTask, default);
        var attempts = log.Entries.Where(x => x.ContainsKey("FirstTextMs")).ToArray();
        Assert.Equal(2, attempts.Length);
        Assert.Equal(category, attempts[0]["Category"]);
        Assert.Null(attempts[0]["FirstTextMs"]);
        Assert.True((double)attempts[1]["FirstTextMs"]! >= 0);
        Assert.Equal("sucesso", attempts[1]["Category"]);
        Assert.Equal("primary", attempts[0]["Model"]);
        Assert.Equal("gemini-3.1-flash-lite", attempts[1]["Model"]);
        Assert.Contains(log.Entries, x => x.ContainsKey("From") && Equals(x["Category"], category));
        Assert.DoesNotContain("Resposta real", log.Text);
        Assert.DoesNotContain("segredo-simulado", log.Text);
    }

    private static GeminiTutorIaProvider Provider(HttpClient http) => new(http,
        new() { ApiKey="fake", Model="primary", FirstTextTimeoutSeconds=1 }, NullLogger<GeminiTutorIaProvider>.Instance);
    private static TutorIaProviderRequest Request() => new(Guid.NewGuid(), PlanoAluno.Foco, "oi", default, new());
    private sealed class Handler(int status, bool partial = false) : HttpMessageHandler
    {
        public List<string> Paths = [];
        public bool Cancelled;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Paths.Add(request.RequestUri!.AbsolutePath);
            if (Paths.Count == 1 && status == 0)
            {
                try { await Task.Delay(Timeout.Infinite, ct); }
                catch (OperationCanceledException) { Cancelled = true; throw; }
            }
            if (Paths.Count == 1 && status != 200)
                return new((HttpStatusCode)status) { Content = new StringContent("{}") };
            var ending = partial ? "" : ",\"finishReason\":\"STOP\"";
            var body = "data: {\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"Resposta real\"}]}" + ending + "}]}\n\n";
            return new(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "text/event-stream") };
        }
    }
}
