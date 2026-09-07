using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorClass.API.Security;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;

namespace TutorClass.API.Controllers;

[ApiController]
[Route("api/tutoria")]
[Authorize(Policy = "Aluno")]
public sealed class TutorIaController : ControllerBase
{
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
    [ProducesResponseType(typeof(TutorIaChatResponse), StatusCodes.Status200OK)]
    public async Task<TutorIaChatResponse> Chat(TutorIaChatRequest request, CancellationToken cancellationToken)
    {
        return await _service.ConversarAsync(User.GetUserId(), request, cancellationToken);
    }
}
