using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TutorClass.API.Security;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;

namespace TutorClass.API.Controllers;

[ApiController]
[Route("api")]
public sealed class QualificacoesController : ControllerBase
{
    private readonly QualificacaoProfessorService _service;

    public QualificacoesController(QualificacaoProfessorService service)
    {
        _service = service;
    }

    [HttpGet("professores/{professorId:guid}/qualificacoes")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyCollection<QualificacaoProfessorDto>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<QualificacaoProfessorDto>> ListarPorProfessor(Guid professorId, CancellationToken cancellationToken)
    {
        return await _service.ListarPorProfessorAsync(professorId, User.GetUserId(), User.IsInRole("Administrador"), cancellationToken);
    }

    [HttpPost("admin/qualificacoes/{qualificacaoId:guid}/verificar")]
    [Authorize(Policy = "Administrador")]
    [ProducesResponseType(typeof(QualificacaoProfessorDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<QualificacaoProfessorDto>> Verificar(Guid qualificacaoId, CancellationToken cancellationToken)
    {
        return Ok(await _service.VerificarAsync(qualificacaoId, User.GetUserId(), cancellationToken));
    }

    [HttpPost("admin/qualificacoes/{qualificacaoId:guid}/rejeitar")]
    [Authorize(Policy = "Administrador")]
    [ProducesResponseType(typeof(QualificacaoProfessorDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<QualificacaoProfessorDto>> Rejeitar(Guid qualificacaoId, CancellationToken cancellationToken)
    {
        return Ok(await _service.RejeitarAsync(qualificacaoId, User.GetUserId(), cancellationToken));
    }
}
