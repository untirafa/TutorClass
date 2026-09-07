using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorClass.API.Security;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;

namespace TutorClass.API.Controllers;

[ApiController]
[Route("api/aulas/{aulaId:guid}/acompanhamento")]
[Authorize]
public sealed class AcompanhamentosController : ControllerBase
{
    private readonly AcompanhamentoAulaService _service;

    public AcompanhamentosController(AcompanhamentoAulaService service)
    {
        _service = service;
    }

    [HttpPut]
    [Authorize(Policy = "Professor")]
    [ProducesResponseType(typeof(AcompanhamentoAulaResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AcompanhamentoAulaResponse>> RegistrarOuAtualizar(
        Guid aulaId,
        RegistrarAcompanhamentoAulaRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _service.RegistrarOuAtualizarAsync(aulaId, User.GetUserId(), request, cancellationToken));
    }

    [HttpGet]
    [ProducesResponseType(typeof(AcompanhamentoAulaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AcompanhamentoAulaResponse>> Buscar(Guid aulaId, CancellationToken cancellationToken)
    {
        var acompanhamento = await _service.BuscarAsync(aulaId, User.GetUserId(), User.IsInRole("Administrador"), cancellationToken);
        return acompanhamento is null ? NotFound() : Ok(acompanhamento);
    }
}
