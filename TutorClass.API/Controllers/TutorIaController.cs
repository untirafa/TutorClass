using TutorClass.API.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Features;
using System.Text.Json;
using TutorClass.API.Security;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;

namespace TutorClass.API.Controllers;

[ApiController]
[Route("api/tutoria")]
[Authorize(Policy = "Aluno")]
public sealed class TutorIaController : ControllerBase
{
    private static readonly JsonSerializerOptions StreamJson = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };
    private readonly TutorIaService _service;

    public TutorIaController(TutorIaService service)
    {
        _service = service;
    }

    [HttpGet("status")]
    [ProducesResponseType(typeof(TutorIaStatusResponse), StatusCodes.Status200OK)]
    public async Task<TutorIaStatusResponse> Status(CancellationToken cancellationToken)
    {
        return await _service.ObterStatusAsync(User.GetUserId(), cancellationToken);
    }

    [HttpPost("chat")]
    [LimiteRequisicoes("tutoria", 12)]
    [ProducesResponseType(typeof(TutorIaChatResponse), StatusCodes.Status200OK)]
    public async Task<TutorIaChatResponse> Chat(TutorIaChatRequest request, CancellationToken cancellationToken)
    {
        TutorIaPerformanceMiddleware.MarcarInicio(HttpContext);
        return await _service.ConversarAsync(User.GetUserId(), request, cancellationToken);
    }

    [HttpPost("chat/stream")]
    [LimiteRequisicoes("tutoria", 12)]
    [Produces("text/event-stream")]
    public async Task Stream(TutorIaChatRequest request, CancellationToken cancellationToken)
    {
        TutorIaPerformanceMiddleware.MarcarInicio(HttpContext);
        async Task Emitir(string evento, object dados, CancellationToken ct)
        {
            if (!Response.HasStarted)
            {
                Response.ContentType = "text/event-stream; charset=utf-8";
                Response.Headers.CacheControl = "no-cache, no-store, no-transform";
                Response.Headers["X-Accel-Buffering"] = "no";
                HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();
            }
            var json = JsonSerializer.Serialize(dados, StreamJson);
            await Response.WriteAsync($"event: {evento}\ndata: {json}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }

        try
        {
            var resposta = await _service.ConversarAsync(User.GetUserId(), request,
                (texto, ct) => Emitir("delta", new { text = texto }, ct), cancellationToken);
            await Emitir("complete", resposta, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception) when (Response.HasStarted && !cancellationToken.IsCancellationRequested)
        {
            await Emitir("error", new { message = "A resposta foi interrompida. Atualize o status antes de tentar novamente." }, cancellationToken);
        }
    }
}
