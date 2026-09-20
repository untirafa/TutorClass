using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorClass.API.Security;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;

namespace TutorClass.API.Controllers;

[ApiController]
[Route("api/materiais")]
[Authorize]
public sealed class MateriaisController : ControllerBase
{
    private readonly MaterialPedagogicoService _service;

    public MateriaisController(MaterialPedagogicoService service)
    {
        _service = service;
    }

    [HttpGet("destinatarios")]
    [Authorize(Policy = "Professor")]
    [ProducesResponseType(typeof(IReadOnlyCollection<DestinatarioMaterialResponse>), StatusCodes.Status200OK)]
    public Task<IReadOnlyCollection<DestinatarioMaterialResponse>> ListarDestinatarios(CancellationToken cancellationToken)
        => _service.ListarDestinatariosAsync(User.GetUserId(), cancellationToken);

    [HttpPost]
    [Authorize(Policy = "Professor")]
    [ProducesResponseType(typeof(MaterialResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<MaterialResponse>> Criar(CriarMaterialRequest request, CancellationToken cancellationToken)
    {
        var material = await _service.CriarAsync(User.GetUserId(), request, cancellationToken);
        return CreatedAtAction(nameof(Buscar), new { id = material.Id }, material);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MaterialResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MaterialResponse>> Buscar(Guid id, CancellationToken cancellationToken)
    {
        var material = await _service.BuscarAsync(
            id,
            User.GetUserId(),
            User.IsInRole("Administrador"),
            User.IsInRole("Responsavel"),
            cancellationToken);

        return material is null ? NotFound() : Ok(material);
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(IReadOnlyCollection<MaterialResponse>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<MaterialResponse>> ListarMeus(CancellationToken cancellationToken)
    {
        return await _service.ListarMeusAsync(
            User.GetUserId(),
            User.IsInRole("Administrador"),
            User.IsInRole("Responsavel"),
            cancellationToken);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Professor")]
    [ProducesResponseType(typeof(MaterialResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MaterialResponse>> Atualizar(Guid id, AtualizarMaterialRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _service.AtualizarAsync(id, User.GetUserId(), request, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Professor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken cancellationToken)
    {
        await _service.ExcluirAsync(id, User.GetUserId(), cancellationToken);
        return NoContent();
    }
}
