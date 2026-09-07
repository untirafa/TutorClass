using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorClass.API.Security;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;

namespace TutorClass.API.Controllers;

[ApiController]
public sealed class AvaliacoesController : ControllerBase
{
    private readonly AvaliacaoProfessorService _service;

    public AvaliacoesController(AvaliacaoProfessorService service)
    {
        _service = service;
    }

    [HttpPost("api/aulas/{aulaId:guid}/avaliacao")]
    [Authorize(Policy = "Aluno")]
    [ProducesResponseType(typeof(AvaliacaoProfessorResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<AvaliacaoProfessorResponse>> Avaliar(
        Guid aulaId,
        CriarAvaliacaoProfessorRequest request,
        CancellationToken cancellationToken)
    {
        var avaliacao = await _service.AvaliarAsync(aulaId, User.GetUserId(), request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, avaliacao);
    }

    [HttpGet("api/professores/{professorId:guid}/avaliacoes")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyCollection<AvaliacaoProfessorResponse>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<AvaliacaoProfessorResponse>> Listar(Guid professorId, CancellationToken cancellationToken)
    {
        return await _service.ListarDoProfessorAsync(professorId, cancellationToken);
    }
}
