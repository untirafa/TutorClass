using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorClass.API.Security;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;

namespace TutorClass.API.Controllers;

[ApiController]
[Route("api/notificacoes")]
[Authorize]
public sealed class NotificacoesController : ControllerBase
{
    private readonly NotificacaoService _service;

    public NotificacoesController(NotificacaoService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<NotificacaoResponse>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<NotificacaoResponse>> Listar(
        [FromQuery] bool somenteNaoLidas = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return await _service.ListarAsync(User.GetUserId(), somenteNaoLidas, page, pageSize, cancellationToken);
    }

    [HttpGet("resumo")]
    [ProducesResponseType(typeof(NotificacaoResumoResponse), StatusCodes.Status200OK)]
    public async Task<NotificacaoResumoResponse> Resumo(CancellationToken cancellationToken)
    {
        return await _service.ObterResumoAsync(User.GetUserId(), cancellationToken);
    }

    [HttpPost("{id:guid}/marcar-lida")]
    [ProducesResponseType(typeof(NotificacaoResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificacaoResponse>> MarcarLida(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _service.MarcarComoLidaAsync(User.GetUserId(), id, cancellationToken));
    }

    [HttpPost("marcar-todas-lidas")]
    [ProducesResponseType(typeof(MarcarTodasNotificacoesLidasResponse), StatusCodes.Status200OK)]
    public async Task<MarcarTodasNotificacoesLidasResponse> MarcarTodasLidas(CancellationToken cancellationToken)
    {
        return await _service.MarcarTodasComoLidasAsync(User.GetUserId(), cancellationToken);
    }
}
