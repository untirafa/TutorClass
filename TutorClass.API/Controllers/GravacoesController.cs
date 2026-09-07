using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorClass.API.Security;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;

namespace TutorClass.API.Controllers;

[ApiController]
[Route("api/gravacoes")]
[Authorize(Policy = "Aluno")]
public sealed class GravacoesController : ControllerBase
{
    private readonly GravacaoAulaService _service;

    public GravacoesController(GravacaoAulaService service)
    {
        _service = service;
    }

    [HttpGet("minhas")]
    [ProducesResponseType(typeof(IReadOnlyCollection<GravacaoAulaResponse>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<GravacaoAulaResponse>> ListarMinhas(CancellationToken cancellationToken)
    {
        return await _service.ListarMinhasAsync(User.GetUserId(), cancellationToken);
    }
}
