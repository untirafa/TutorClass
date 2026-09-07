using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;

namespace TutorClass.API.Controllers;

[ApiController]
[Route("api/admin/faq")]
[Authorize(Policy = "Administrador")]
public sealed class AdminFaqController : ControllerBase
{
    private readonly FaqService _service;

    public AdminFaqController(FaqService service)
    {
        _service = service;
    }

    [HttpPost]
    [ProducesResponseType(typeof(FaqResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<FaqResponse>> Criar(CriarFaqRequest request, CancellationToken cancellationToken)
    {
        var faq = await _service.CriarAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, faq);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(FaqResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<FaqResponse>> Atualizar(Guid id, AtualizarFaqRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _service.AtualizarAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/desativar")]
    [ProducesResponseType(typeof(FaqResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<FaqResponse>> Desativar(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _service.DesativarAsync(id, cancellationToken));
    }

    [HttpPost("{id:guid}/ativar")]
    [ProducesResponseType(typeof(FaqResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<FaqResponse>> Ativar(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _service.AtivarAsync(id, cancellationToken));
    }
}
