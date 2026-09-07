using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorClass.API.Security;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;

namespace TutorClass.API.Controllers;

[ApiController]
[Route("api/aulas")]
public sealed class AulasController : ControllerBase
{
    private readonly AulaService _service;
    private readonly GravacaoAulaService _gravacoes;

    public AulasController(AulaService service, GravacaoAulaService gravacoes)
    {
        _service = service;
        _gravacoes = gravacoes;
    }

    [HttpPost]
    [Authorize(Policy = "Aluno")]
    [ProducesResponseType(typeof(AulaDetalhesResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<AulaDetalhesResponse>> Solicitar(SolicitarAulaRequest request, CancellationToken cancellationToken)
    {
        var aula = await _service.SolicitarAsync(User.GetUserId(), request, cancellationToken);
        return CreatedAtAction(nameof(Buscar), new { id = aula.Id }, aula);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(AulaDetalhesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AulaDetalhesResponse>> Buscar(Guid id, CancellationToken cancellationToken)
    {
        var aula = await _service.BuscarAsync(id, User.GetUserId(), User.IsInRole("Administrador"), cancellationToken);
        return aula is null ? NotFound() : Ok(aula);
    }

    [HttpGet("minhas")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyCollection<AulaResumoResponse>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<AulaResumoResponse>> ListarMinhas(CancellationToken cancellationToken)
    {
        return await _service.ListarMinhasAsync(User.GetUserId(), User.IsInRole("Administrador"), cancellationToken);
    }

    [HttpPost("{id:guid}/confirmar")]
    [Authorize(Policy = "Professor")]
    [ProducesResponseType(typeof(AulaDetalhesResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AulaDetalhesResponse>> Confirmar(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _service.ConfirmarAsync(id, User.GetUserId(), cancellationToken));
    }

    [HttpPost("{id:guid}/cancelar")]
    [Authorize]
    [ProducesResponseType(typeof(AulaDetalhesResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AulaDetalhesResponse>> Cancelar(Guid id, CancelarAulaRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _service.CancelarAsync(id, User.GetUserId(), request.Motivo, cancellationToken));
    }

    [HttpPut("{id:guid}/reagendar")]
    [Authorize]
    [ProducesResponseType(typeof(AulaDetalhesResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AulaDetalhesResponse>> Reagendar(Guid id, ReagendarAulaRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _service.ReagendarAsync(id, User.GetUserId(), request, cancellationToken));
    }

    [HttpGet("{id:guid}/reagendamentos")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyCollection<AulaReagendamentoResponse>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<AulaReagendamentoResponse>> ListarReagendamentos(Guid id, CancellationToken cancellationToken)
    {
        return await _service.ListarReagendamentosAsync(id, User.GetUserId(), User.IsInRole("Administrador"), cancellationToken);
    }

    [HttpPost("{id:guid}/concluir")]
    [Authorize(Policy = "Professor")]
    [ProducesResponseType(typeof(AulaDetalhesResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AulaDetalhesResponse>> Concluir(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _service.ConcluirAsync(id, User.GetUserId(), cancellationToken));
    }

    [HttpPost("{id:guid}/gravacao")]
    [Authorize(Policy = "Administrador")]
    [ProducesResponseType(typeof(GravacaoAulaResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<GravacaoAulaResponse>> CriarGravacao(Guid id, CriarGravacaoAulaRequest request, CancellationToken cancellationToken)
    {
        var gravacao = await _gravacoes.CriarAsync(id, User.GetUserId(), User.IsInRole("Administrador"), request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, gravacao);
    }

    [HttpPut("{id:guid}/gravacao")]
    [Authorize(Policy = "Administrador")]
    [ProducesResponseType(typeof(GravacaoAulaResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GravacaoAulaResponse>> AtualizarGravacao(Guid id, CriarGravacaoAulaRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _gravacoes.AtualizarAsync(id, User.GetUserId(), User.IsInRole("Administrador"), request, cancellationToken));
    }

    [HttpPost("{id:guid}/gravacao/desativar")]
    [Authorize(Policy = "Administrador")]
    [ProducesResponseType(typeof(GravacaoAulaResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GravacaoAulaResponse>> DesativarGravacao(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _gravacoes.DesativarAsync(id, User.GetUserId(), User.IsInRole("Administrador"), cancellationToken));
    }

    [HttpGet("{id:guid}/gravacao")]
    [Authorize(Policy = "Aluno")]
    [ProducesResponseType(typeof(GravacaoAulaResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GravacaoAulaResponse>> BuscarGravacao(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _gravacoes.BuscarParaAlunoAsync(id, User.GetUserId(), cancellationToken));
    }
}
