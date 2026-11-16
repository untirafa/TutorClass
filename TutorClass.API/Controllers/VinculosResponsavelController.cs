using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorClass.API.Security;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;
using TutorClass.Domain.Users;

namespace TutorClass.API.Controllers;

[ApiController]
[Route("api")]
public sealed class VinculosResponsavelController : ControllerBase
{
    private readonly VinculoResponsavelService _service;

    public VinculosResponsavelController(VinculoResponsavelService service)
    {
        _service = service;
    }

    [HttpPost("responsavel/solicitacoes-vinculo")]
    [Authorize(Policy = "Responsavel")]
    [ProducesResponseType(typeof(SolicitacaoVinculoResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<SolicitacaoVinculoResponse>> Solicitar(SolicitarVinculoRequest request, CancellationToken cancellationToken)
    {
        var solicitacao = await _service.SolicitarAsync(User.GetUserId(), request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, solicitacao);
    }

    [HttpGet("responsavel/solicitacoes-vinculo")]
    [Authorize(Policy = "Responsavel")]
    [ProducesResponseType(typeof(IReadOnlyCollection<SolicitacaoVinculoResponse>), StatusCodes.Status200OK)]
    public Task<IReadOnlyCollection<SolicitacaoVinculoResponse>> Minhas(CancellationToken cancellationToken)
    {
        return _service.ListarMinhasAsync(User.GetUserId(), cancellationToken);
    }

    [HttpGet("admin/solicitacoes-vinculo")]
    [Authorize(Policy = "Administrador")]
    [ProducesResponseType(typeof(IReadOnlyCollection<SolicitacaoVinculoAdminResponse>), StatusCodes.Status200OK)]
    public Task<IReadOnlyCollection<SolicitacaoVinculoAdminResponse>> ListarAdmin([FromQuery] StatusSolicitacaoVinculo? status, CancellationToken cancellationToken)
    {
        return _service.ListarAdminAsync(User.GetUserId(), status, cancellationToken);
    }

    [HttpPost("admin/solicitacoes-vinculo/{solicitacaoId:guid}/aprovar")]
    [Authorize(Policy = "Administrador")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Aprovar(Guid solicitacaoId, AprovarVinculoRequest request, CancellationToken cancellationToken)
    {
        await _service.AprovarAsync(solicitacaoId, User.GetUserId(), request, cancellationToken);
        return NoContent();
    }

    [HttpPost("admin/solicitacoes-vinculo/{solicitacaoId:guid}/recusar")]
    [Authorize(Policy = "Administrador")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Recusar(Guid solicitacaoId, RecusarVinculoRequest request, CancellationToken cancellationToken)
    {
        await _service.RecusarAsync(solicitacaoId, User.GetUserId(), request, cancellationToken);
        return NoContent();
    }
}
