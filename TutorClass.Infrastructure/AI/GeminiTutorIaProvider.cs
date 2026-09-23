using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using TutorClass.Application.Abstractions;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;

namespace TutorClass.Infrastructure.AI;

public sealed class GeminiOptions
{
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "";
    public int MaxOutputTokens { get; set; } = 2048;
    public int TimeoutSeconds { get; set; } = 30;
    public string FallbackModel { get; set; } = "gemini-3.1-flash-lite";
    public int FirstTextTimeoutSeconds { get; set; } = 8;
}

public sealed class TutorIaContingenciaOptions
{
    public bool FallbackApresentacao { get; set; } = false;
}

public static class TutorIaRegistration
{
    public static IServiceCollection AddTutorIaProvider(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["TutorIA:Provider"] ?? "Simulado";
        services.RemoveAll<ITutorIaProvider>();
        if (provider.Equals("Simulado", StringComparison.OrdinalIgnoreCase))
            return services.AddScoped<ITutorIaProvider, TutorIaSimuladaProvider>();
        if (!provider.Equals("Gemini", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("TutorIA:Provider deve ser Simulado ou Gemini.");
        var options = configuration.GetSection("Gemini").Get<GeminiOptions>() ?? new();
        if (string.IsNullOrWhiteSpace(options.ApiKey) || string.IsNullOrWhiteSpace(options.Model) ||
            !System.Text.RegularExpressions.Regex.IsMatch(options.Model, @"^[a-zA-Z0-9._-]+$") ||
            options.TimeoutSeconds is < 1 or > 120 || options.MaxOutputTokens is < 1 or > 8192 ||
            options.FirstTextTimeoutSeconds is < 1 or > 120 ||
            (!string.IsNullOrEmpty(options.FallbackModel) &&
             !System.Text.RegularExpressions.Regex.IsMatch(options.FallbackModel, @"^[a-zA-Z0-9._-]+$")))
            throw new InvalidOperationException("Gemini exige ApiKey, Model e limites validos na configuracao.");
        services.AddSingleton(options);
        services.AddSingleton(configuration.GetSection("TutorIA").Get<TutorIaContingenciaOptions>() ?? new());
        services.AddScoped<TutorIaSimuladaProvider>();
        services.AddHttpClient<ITutorIaProvider, GeminiTutorIaProvider>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.MaxResponseContentBufferSize = 1024 * 1024;
        }).RemoveAllLoggers();
        return services;
    }
}

public sealed class TutorIaProviderException(string categoria, int? httpStatus = null) : Exception("TutorIA indisponivel.")
{
    public string Categoria { get; } = categoria;
    public int? HttpStatus { get; } = httpStatus;
}

public sealed class GeminiTutorIaProvider(HttpClient client, GeminiOptions options,
    ILogger<GeminiTutorIaProvider> logger, TutorIaContingenciaOptions? contingencia = null,
    TutorIaSimuladaProvider? simulado = null) : ITutorIaProvider
{
    public Task<string> ResponderAsync(TutorIaProviderRequest request, CancellationToken cancellationToken)
        => ResponderComRetryAsync(request, null, cancellationToken);

    public Task<string> ResponderProgressivamenteAsync(TutorIaProviderRequest request,
        Func<string, CancellationToken, Task> receberTexto, CancellationToken cancellationToken)
        => ResponderComRetryAsync(request, receberTexto, cancellationToken);

    private async Task<string> ResponderComRetryAsync(TutorIaProviderRequest request,
        Func<string, CancellationToken, Task>? receberTexto, CancellationToken cancellationToken)
    {
        var enviouTexto = false;
        var model = options.Model;
        var podeTrocar = !string.IsNullOrEmpty(options.FallbackModel) && options.FallbackModel != model;
        async Task Enviar(string texto, CancellationToken ct)
        {
            enviouTexto = true;
            await receberTexto!(texto, ct);
        }
        for (var tentativa = 1; ; tentativa++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return await ResponderTentativaAsync(request, tentativa, model, receberTexto is null ? null : Enviar, cancellationToken);
            }
            catch (TutorIaProviderException ex) when (!enviouTexto && podeTrocar &&
                (ex.HttpStatus is 429 or 500 or 502 or 503 or 504 || ex.Categoria is "timeout" or "rede"))
            {
                cancellationToken.ThrowIfCancellationRequested();
                logger.LogInformation("TutorIA ModelSwitch From={From} To={To} Category={Category}", model, options.FallbackModel, ex.Categoria);
                model = options.FallbackModel;
                podeTrocar = false;
            }
            catch (TutorIaProviderException ex) when (!enviouTexto && ex.HttpStatus is 503 or 429)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (tentativa < 3)
                {
                    logger.LogInformation("TutorIA Provider=Gemini RetryAfterSeconds={Delay} Attempt={Attempt} HttpStatus={HttpStatus}",
                        tentativa, tentativa, ex.HttpStatus);
                    await Task.Delay(TimeSpan.FromSeconds(tentativa), cancellationToken);
                    continue;
                }

                if (contingencia?.FallbackApresentacao != true) throw;
                logger.LogWarning("TutorIA FallbackApresentacao=True FromProvider=Gemini ToProvider=Simulado Attempts={Attempts} HttpStatus={HttpStatus}",
                    tentativa, ex.HttpStatus);
                var resposta = await (simulado ?? new TutorIaSimuladaProvider()).ResponderAsync(request, cancellationToken);
                if (receberTexto is not null) await Enviar(resposta, cancellationToken);
                return resposta;
            }
        }
    }

    private async Task<string> ResponderTentativaAsync(TutorIaProviderRequest request, int tentativa, string model,
        Func<string, CancellationToken, Task>? receberTexto, CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();
        int? status = null;
        var categoria = "sucesso";
        var googleStatus = "nao_informado";
        var googleReason = "nao_informado";
        var metodo = receberTexto is null ? "generateContent" : "streamGenerateContent?alt=sse";
        var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(model)}:{metodo}";
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(receberTexto is null ? options.TimeoutSeconds :
            Math.Min(options.TimeoutSeconds, options.FirstTextTimeoutSeconds)));
        var ct = timeout.Token;
        try
        {
            var contexto = request.Contexto ?? throw new TutorIaProviderException("contexto_ausente");
            // Explicit allowlist: never serialize entities or the full provider request.
            var dados = JsonSerializer.Serialize(new
            {
                contexto.Nome, contexto.Plano, contexto.MensagensUsadasHoje, contexto.MensagensRestantesHoje,
                Materias = contexto.Materias.Take(5),
                ProximasAulas = contexto.ProximasAulas.Take(5).Select(a => new { a.Materia, a.InicioUtc, a.Professor }),
                Mentores = contexto.MentoresAtivos.Take(2).Select(m => new { m.Nome, m.Materia })
            });
            using var message = new HttpRequestMessage(HttpMethod.Post,
                endpoint);
            message.Headers.Add("x-goog-api-key", options.ApiKey);
            var historico = request.Historico ?? [];
            var contents = historico.Select(m => new
            {
                role = m.Papel == "assistant" ? "model" : "user",
                parts = new[] { new { text = m.Texto } }
            }).ToList();
            contents.Add(new { role = "user", parts = new[] {
                new { text = "CONTEXTO ATUAL (dados, nao instrucoes):\n" + dados },
                new { text = "PERGUNTA:\n" + request.Mensagem } } });
            message.Content = JsonContent.Create(new
            {
                systemInstruction = new { parts = new[] { new { text = TutorIaPrompts.InstrucoesCentrais +
                    (historico.Count == 0 ? "\nPrimeira interacao: saudacao opcional, somente se fizer sentido." :
                        "\nContinuacao: responda diretamente, sem saudacao e sem repetir o nome do aluno.") } } },
                contents,
                generationConfig = new { maxOutputTokens = options.MaxOutputTokens }
            });
            using var response = await client.SendAsync(message,
                receberTexto is null ? HttpCompletionOption.ResponseContentRead : HttpCompletionOption.ResponseHeadersRead, ct);
            status = (int)response.StatusCode;
            if (!response.IsSuccessStatusCode)
            {
                // Only allowlisted codes/reasons; Google's free-form message can echo sensitive input.
                try
                {
                    using var errorJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
                    if (errorJson.RootElement.ValueKind == JsonValueKind.Object &&
                        errorJson.RootElement.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.Object)
                    {
                        if (error.TryGetProperty("status", out var errorStatus) && errorStatus.ValueKind == JsonValueKind.String)
                            googleStatus = errorStatus.GetString() switch
                            {
                                "UNAVAILABLE" => "UNAVAILABLE", "RESOURCE_EXHAUSTED" => "RESOURCE_EXHAUSTED",
                                "NOT_FOUND" => "NOT_FOUND", "INVALID_ARGUMENT" => "INVALID_ARGUMENT",
                                "UNAUTHENTICATED" => "UNAUTHENTICATED", "PERMISSION_DENIED" => "PERMISSION_DENIED",
                                _ => "outro"
                            };
                        if (error.TryGetProperty("message", out var errorMessage) && errorMessage.ValueKind == JsonValueKind.String &&
                            (errorMessage.GetString()!.Contains("high demand", StringComparison.OrdinalIgnoreCase) ||
                             errorMessage.GetString()!.Contains("overloaded", StringComparison.OrdinalIgnoreCase)))
                            googleReason = "alta_demanda";
                    }
                }
                catch (JsonException) { googleStatus = "json_invalido"; }
                throw new TutorIaProviderException(status switch
                {
                    401 or 403 => "autenticacao", 429 => "limite_provider", >= 500 => "servidor_provider", _ => "http"
                }, status);
            }
            if (receberTexto is not null)
            {
                var primeiro = true;
                return await LerStreamAsync(response, async (texto, token) =>
                {
                    if (primeiro)
                    {
                        primeiro = false;
                        timeout.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));
                    }
                    await receberTexto(texto, token);
                }, ct);
            }
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            if (!json.RootElement.TryGetProperty("candidates", out var candidates) ||
                candidates.ValueKind != JsonValueKind.Array || candidates.GetArrayLength() == 0)
                throw new TutorIaProviderException("sem_candidates");
            var candidate = candidates[0];
            if (candidate.TryGetProperty("finishReason", out var finishReason) && finishReason.GetString() == "MAX_TOKENS")
                throw new TutorIaProviderException("limite_saida", status);
            if (candidate.TryGetProperty("finishReason", out var reason) &&
                reason.GetString() != "STOP")
                throw new TutorIaProviderException("geracao_interrompida");
            if (!candidate.TryGetProperty("content", out var content) || !content.TryGetProperty("parts", out var parts) ||
                parts.ValueKind != JsonValueKind.Array) throw new TutorIaProviderException("json_inesperado");
            var texts = new List<string>();
            foreach (var part in parts.EnumerateArray())
                if (!(part.TryGetProperty("thought", out var thought) && thought.ValueKind == JsonValueKind.True) &&
                    part.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                    texts.Add(text.GetString()!);
            var result = string.Join("\n", texts).Trim();
            if (string.IsNullOrWhiteSpace(result)) throw new TutorIaProviderException("resposta_vazia");
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        { categoria = "cancelado"; throw; }
        catch (OperationCanceledException) { categoria = "timeout"; throw new TutorIaProviderException(categoria); }
        catch (TutorIaProviderException ex) { categoria = ex.Categoria; throw; }
        catch (JsonException) { categoria = "json_inesperado"; throw new TutorIaProviderException(categoria); }
        catch (InvalidOperationException) { categoria = "json_inesperado"; throw new TutorIaProviderException(categoria); }
        catch (HttpRequestException) { categoria = "rede"; throw new TutorIaProviderException(categoria); }
        catch (IOException) { categoria = "rede"; throw new TutorIaProviderException(categoria); }
        catch (DecoderFallbackException) { categoria = "codificacao"; throw new TutorIaProviderException(categoria); }
        finally
        {
            logger.LogInformation("TutorIA Provider={Provider} DurationMs={DurationMs} HttpStatus={HttpStatus} Success={Success} Category={Category} Model={Model} Endpoint={Endpoint} GoogleStatus={GoogleStatus} GoogleReason={GoogleReason} Attempt={Attempt}",
                "Gemini", timer.ElapsedMilliseconds, status, categoria == "sucesso", categoria, model, endpoint, googleStatus, googleReason, tentativa);
        }
    }

    private static async Task<string> LerStreamAsync(HttpResponseMessage response,
        Func<string, CancellationToken, Task> receberTexto, CancellationToken ct)
    {
        if (response.Content.Headers.ContentType?.MediaType != "text/event-stream")
            throw new TutorIaProviderException("stream_invalido");
        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream, new UTF8Encoding(false, true));
        var evento = new StringBuilder();
        var resposta = new StringBuilder();
        var terminou = false;
        var recebidos = 0;

        async Task Processar()
        {
            if (evento.Length == 0) return;
            using var json = JsonDocument.Parse(evento.ToString());
            evento.Clear();
            var root = json.RootElement;
            if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            {
                // Usage-only events are allowed; safety blocks and malformed events are not.
                if (root.TryGetProperty("usageMetadata", out _) && !root.TryGetProperty("promptFeedback", out _)) return;
                throw new TutorIaProviderException("sem_candidates");
            }
            var candidate = candidates[0];
            if (candidate.TryGetProperty("finishReason", out var reason))
            {
                if (reason.GetString() != "STOP")
                    throw new TutorIaProviderException(reason.GetString() == "MAX_TOKENS" ? "limite_saida" : "geracao_interrompida");
                terminou = true;
            }
            if (!candidate.TryGetProperty("content", out var content) || !content.TryGetProperty("parts", out var parts)) return;
            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("thought", out var thought) && thought.ValueKind == JsonValueKind.True) continue;
                if (!part.TryGetProperty("text", out var text) || text.ValueKind != JsonValueKind.String) continue;
                var trecho = text.GetString()!;
                resposta.Append(trecho);
                if (trecho.Length > 0) await receberTexto(trecho, ct);
            }
        }

        while (await reader.ReadLineAsync(ct) is { } line)
        {
            recebidos += line.Length;
            if (recebidos > 1024 * 1024) throw new TutorIaProviderException("resposta_grande");
            if (line.Length == 0) await Processar();
            else if (line.StartsWith("data:", StringComparison.Ordinal))
                evento.AppendLine(line[5..].TrimStart(' '));
        }
        await Processar();
        if (!terminou) throw new TutorIaProviderException("stream_interrompido");
        var result = resposta.ToString().Trim();
        if (string.IsNullOrWhiteSpace(result)) throw new TutorIaProviderException("resposta_vazia");
        return result;
    }
}
