using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorClass.API.Security;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;

namespace TutorClass.API.Controllers;

/// <summary>Denuncias feitas pelas pessoas usuarias (aluno, responsavel, professor).</summary>
[ApiController]
[Route("api/denuncias")]
[Authorize]
public sealed class DenunciasController : ControllerBase
{
    private readonly DenunciaService _service;

    public DenunciasController(DenunciaService service)
    {
        _service = service;
    }

    [HttpPost]
    [ProducesResponseType(typeof(DenunciaResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<DenunciaResponse>> Criar(CriarDenunciaRequest request, CancellationToken cancellationToken)
    {
        var denuncia = await _service.CriarAsync(User.GetUserId(), request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, denuncia);
    }

    [HttpGet("minhas")]
    [ProducesResponseType(typeof(IReadOnlyCollection<DenunciaResponse>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<DenunciaResponse>> Minhas(CancellationToken cancellationToken)
    {
        return await _service.ListarMinhasAsync(User.GetUserId(), cancellationToken);
    }
}
