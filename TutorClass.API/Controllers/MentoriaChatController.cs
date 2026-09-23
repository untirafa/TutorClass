using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TutorClass.API.Hubs;
using TutorClass.API.Security;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;

namespace TutorClass.API.Controllers;

[ApiController]
[Route("api/mentorias/{vinculoMentoriaId:guid}/chat")]
[Authorize]
public sealed class MentoriaChatController : ControllerBase
{
    private readonly MentoriaChatService _service;
    private readonly IHubContext<MentoriaChatHub> _hubContext;

    public MentoriaChatController(MentoriaChatService service, IHubContext<MentoriaChatHub> hubContext)
    {
        _service = service;
        _hubContext = hubContext;
    }

    [HttpGet]
    [ProducesResponseType(typeof(MentoriaChatResponse), StatusCodes.Status200OK)]
    public async Task<MentoriaChatResponse> ObterChat(Guid vinculoMentoriaId, CancellationToken cancellationToken)
    {
        return await _service.ObterChatAsync(vinculoMentoriaId, User.GetUserId(), cancellationToken);
    }

    [HttpGet("mensagens")]
    [ProducesResponseType(typeof(MensagensMentoriaPageResponse), StatusCodes.Status200OK)]
    public async Task<MensagensMentoriaPageResponse> ListarMensagens(Guid vinculoMentoriaId, [FromQuery] int? limit, [FromQuery] DateTimeOffset? before, CancellationToken cancellationToken)
    {
        return await _service.ListarMensagensAsync(vinculoMentoriaId, User.GetUserId(), limit, before?.UtcDateTime, cancellationToken);
    }

    [HttpPost("mensagens")]
    [LimiteRequisicoes("mentoria", 60)]
    [ProducesResponseType(typeof(MensagemMentoriaResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<MensagemMentoriaResponse>> EnviarMensagem(Guid vinculoMentoriaId, EnviarMensagemMentoriaRequest request, CancellationToken cancellationToken)
    {
        var mensagem = await _service.EnviarMensagemAsync(vinculoMentoriaId, User.GetUserId(), request, cancellationToken);
        await _hubContext.Clients.Group(MentoriaChatHub.GroupName(vinculoMentoriaId)).SendAsync("MensagemRecebida", mensagem, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, mensagem);
    }

    [HttpPost("mensagens/lidas")]
    [ProducesResponseType(typeof(MensagensLidasResponse), StatusCodes.Status200OK)]
    public async Task<MensagensLidasResponse> MarcarComoLidas(Guid vinculoMentoriaId, CancellationToken cancellationToken)
    {
        return await _service.MarcarComoLidasAsync(vinculoMentoriaId, User.GetUserId(), cancellationToken);
    }
}
