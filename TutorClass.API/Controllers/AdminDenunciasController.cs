using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorClass.API.Security;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;
using TutorClass.Domain.Moderation;

namespace TutorClass.API.Controllers;

/// <summary>Fila de moderacao: analise das denuncias, sancoes e reativacao de contas suspensas.</summary>
[ApiController]
[Route("api/admin/denuncias")]
[Authorize(Policy = "Administrador")]
public sealed class AdminDenunciasController : ControllerBase
{
    private readonly DenunciaService _service;

    public AdminDenunciasController(DenunciaService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<DenunciaAdminResponse>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<DenunciaAdminResponse>> Listar(
        [FromQuery] StatusDenuncia? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30,
        CancellationToken cancellationToken = default)
    {
        return await _service.ListarAdminAsync(status, page, pageSize, cancellationToken);
    }

    [HttpGet("resumo")]
    [ProducesResponseType(typeof(DenunciasResumoResponse), StatusCodes.Status200OK)]
    public async Task<DenunciasResumoResponse> Resumo(CancellationToken cancellationToken)
    {
        return await _service.ObterResumoAsync(cancellationToken);
    }

    [HttpPost("{id:guid}/iniciar-analise")]
    [ProducesResponseType(typeof(DenunciaAdminResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DenunciaAdminResponse>> IniciarAnalise(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _service.IniciarAnaliseAsync(id, User.GetUserId(), cancellationToken));
    }

    [HttpPost("{id:guid}/decidir")]
    [ProducesResponseType(typeof(DenunciaAdminResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DenunciaAdminResponse>> Decidir(Guid id, DecidirDenunciaRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _service.DecidirAsync(id, User.GetUserId(), request, cancellationToken));
    }

    [HttpPost("{id:guid}/reativar-conta")]
    [ProducesResponseType(typeof(DenunciaAdminResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DenunciaAdminResponse>> ReativarConta(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _service.ReativarContaAsync(id, User.GetUserId(), cancellationToken));
    }
}
