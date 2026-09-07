using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TutorClass.API.Security;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;

namespace TutorClass.API.Controllers;

[ApiController]
[Route("api")]
public sealed class CandidaturasProfessorController : ControllerBase
{
    private readonly CandidaturaProfessorService _service;

    public CandidaturasProfessorController(CandidaturaProfessorService service)
    {
        _service = service;
    }

    [HttpPost("professores/{professorId:guid}/candidaturas")]
    [Authorize(Policy = "Professor")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Enviar(Guid professorId, EnviarCandidaturaProfessorRequest request, CancellationToken cancellationToken)
    {
        if (User.GetUserId() != professorId)
        {
            return Forbid();
        }

        var id = await _service.EnviarAsync(professorId, request, cancellationToken);
        return CreatedAtAction(nameof(Buscar), new { candidaturaId = id }, id);
    }

    [HttpGet("candidaturas-professor/{candidaturaId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(CandidaturaProfessorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CandidaturaProfessorDto>> Buscar(Guid candidaturaId, CancellationToken cancellationToken)
    {
        var candidatura = await _service.BuscarAsync(candidaturaId, User.GetUserId(), User.IsInRole("Administrador"), cancellationToken);
        return candidatura is null ? NotFound() : Ok(candidatura);
    }

    [HttpPost("admin/candidaturas-professor/{candidaturaId:guid}/aprovar")]
    [Authorize(Policy = "Administrador")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Aprovar(Guid candidaturaId, AdminCandidaturaDecisionRequest request, CancellationToken cancellationToken)
    {
        await _service.AprovarAsync(candidaturaId, User.GetUserId(), cancellationToken);
        return NoContent();
    }

    [HttpPost("admin/candidaturas-professor/{candidaturaId:guid}/iniciar-analise")]
    [Authorize(Policy = "Administrador")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> IniciarAnalise(Guid candidaturaId, AdminCandidaturaDecisionRequest request, CancellationToken cancellationToken)
    {
        await _service.IniciarAnaliseAsync(candidaturaId, User.GetUserId(), cancellationToken);
        return NoContent();
    }

    [HttpPost("admin/candidaturas-professor/{candidaturaId:guid}/reprovar")]
    [Authorize(Policy = "Administrador")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Reprovar(Guid candidaturaId, AdminCandidaturaDecisionRequest request, CancellationToken cancellationToken)
    {
        await _service.ReprovarAsync(candidaturaId, new DecidirCandidaturaProfessorRequest(User.GetUserId(), request.Motivo), cancellationToken);
        return NoContent();
    }

    [HttpPost("admin/candidaturas-professor/{candidaturaId:guid}/solicitar-complementacao")]
    [Authorize(Policy = "Administrador")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SolicitarComplementacao(Guid candidaturaId, AdminCandidaturaDecisionRequest request, CancellationToken cancellationToken)
    {
        await _service.SolicitarComplementacaoAsync(candidaturaId, new DecidirCandidaturaProfessorRequest(User.GetUserId(), request.Motivo), cancellationToken);
        return NoContent();
    }
}
