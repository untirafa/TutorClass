using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;

namespace TutorClass.API.Controllers;

[ApiController]
[Route("api/faq")]
[AllowAnonymous]
public sealed class FaqController : ControllerBase
{
    private readonly FaqService _service;

    public FaqController(FaqService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<FaqResponse>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<FaqResponse>> Listar([FromQuery] string? categoria, [FromQuery] string? termo, CancellationToken cancellationToken)
    {
        return await _service.ListarAsync(categoria, termo, cancellationToken);
    }

    [HttpPost("chat")]
    [ProducesResponseType(typeof(FaqChatResponse), StatusCodes.Status200OK)]
    public async Task<FaqChatResponse> Chat(FaqChatRequest request, CancellationToken cancellationToken)
    {
        return await _service.ConversarAsync(request, cancellationToken);
    }
}
