using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorClass.API.Security;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;

namespace TutorClass.API.Controllers;

[ApiController]
[Route("api/mentorias")]
[Authorize]
public sealed class MentoriasController : ControllerBase
{
    private readonly MentoriaService _service;

    public MentoriasController(MentoriaService service)
    {
        _service = service;
    }

    [HttpPost("me")]
    [Authorize(Policy = "Aluno")]
    [ProducesResponseType(typeof(MentoriaResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<MentoriaResponse>> SelecionarMentor(SelecionarMentorRequest request, CancellationToken cancellationToken)
    {
        var mentoria = await _service.SelecionarMentorAsync(User.GetUserId(), request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, mentoria);
    }

    [HttpPut("me/slots/{slot:int}/trocar")]
    [Authorize(Policy = "Aluno")]
    [ProducesResponseType(typeof(MentoriaResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MentoriaResponse>> TrocarMentor(int slot, TrocarMentorRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _service.TrocarMentorAsync(User.GetUserId(), slot, request, cancellationToken));
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(IReadOnlyCollection<MentoriaResponse>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<MentoriaResponse>> ListarMinhas(CancellationToken cancellationToken)
    {
        return await _service.ListarMinhasMentoriasAsync(User.GetUserId(), User.IsInRole("Professor"), cancellationToken);
    }

    [HttpGet("acompanhamentos/pendentes")]
    [ProducesResponseType(typeof(IReadOnlyCollection<AcompanhamentoMentoriaResponse>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<AcompanhamentoMentoriaResponse>> ListarPendentes(CancellationToken cancellationToken)
    {
        return await _service.ListarAcompanhamentosAsync(User.GetUserId(), User.IsInRole("Professor"), somentePendentes: true, cancellationToken);
    }

    [HttpGet("acompanhamentos/historico")]
    [ProducesResponseType(typeof(IReadOnlyCollection<AcompanhamentoMentoriaResponse>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<AcompanhamentoMentoriaResponse>> Historico(CancellationToken cancellationToken)
    {
        return await _service.ListarAcompanhamentosAsync(User.GetUserId(), User.IsInRole("Professor"), somentePendentes: false, cancellationToken);
    }

    [HttpPut("acompanhamentos/{acompanhamentoId:guid}")]
    [Authorize(Policy = "Professor")]
    [ProducesResponseType(typeof(AcompanhamentoMentoriaResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AcompanhamentoMentoriaResponse>> AtualizarAcompanhamento(Guid acompanhamentoId, AtualizarAcompanhamentoMentoriaRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _service.AtualizarAcompanhamentoAsync(acompanhamentoId, User.GetUserId(), request, cancellationToken));
    }

    [HttpPost("acompanhamentos/{acompanhamentoId:guid}/concluir")]
    [Authorize(Policy = "Professor")]
    [ProducesResponseType(typeof(ConclusaoAcompanhamentoMentoriaResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ConclusaoAcompanhamentoMentoriaResponse>> ConcluirAcompanhamento(Guid acompanhamentoId, AtualizarAcompanhamentoMentoriaRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _service.ConcluirAcompanhamentoAsync(acompanhamentoId, User.GetUserId(), request, cancellationToken));
    }
}
