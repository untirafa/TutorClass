using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TutorClass.API.Security;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;

namespace TutorClass.API.Controllers;

[ApiController]
[Route("api/professores")]
public sealed class ProfessoresController : ControllerBase
{
    private readonly ProfessorService _service;
    private readonly DisponibilidadeProfessorService _disponibilidades;

    public ProfessoresController(ProfessorService service, DisponibilidadeProfessorService disponibilidades)
    {
        _service = service;
        _disponibilidades = disponibilidades;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyCollection<ProfessorResumoDto>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<ProfessorResumoDto>> ListarAprovados([FromQuery] Guid? materiaId, CancellationToken cancellationToken)
    {
        return await _service.ListarAprovadosAsync(materiaId, cancellationToken);
    }

    [HttpGet("{professorId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProfessorDetalhesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfessorDetalhesDto>> Buscar(Guid professorId, CancellationToken cancellationToken)
    {
        var professor = await _service.BuscarPublicoPorIdAsync(professorId, cancellationToken);
        return professor is null ? NotFound() : Ok(professor);
    }

    [HttpPost]
    [Authorize(Policy = "Administrador")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Criar(CriarProfessorRequest request, CancellationToken cancellationToken)
    {
        var id = await _service.CriarProfessorAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Buscar), new { professorId = id }, id);
    }

    [HttpPost("{professorId:guid}/materias")]
    [Authorize(Policy = "Professor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> VincularMateria(Guid professorId, VincularMateriaProfessorRequest request, CancellationToken cancellationToken)
    {
        if (User.GetUserId() != professorId)
        {
            return Forbid();
        }

        await _service.VincularMateriaAsync(professorId, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("{professorId:guid}/qualificacoes")]
    [Authorize(Policy = "Professor")]
    [ProducesResponseType(typeof(QualificacaoProfessorDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<QualificacaoProfessorDto>> AdicionarQualificacao(Guid professorId, AdicionarQualificacaoProfessorRequest request, CancellationToken cancellationToken)
    {
        if (User.GetUserId() != professorId)
        {
            return Forbid();
        }

        var qualificacao = await _service.AdicionarQualificacaoAsync(professorId, request, cancellationToken);
        return Created($"/api/professores/{professorId}/qualificacoes/{qualificacao.Id}", qualificacao);
    }

    [HttpPost("{professorId:guid}/disponibilidades")]
    [Authorize(Policy = "Professor")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> CadastrarDisponibilidade(Guid professorId, CadastrarDisponibilidadeProfessorRequest request, CancellationToken cancellationToken)
    {
        if (User.GetUserId() != professorId)
        {
            return Forbid();
        }

        var disponibilidade = await _disponibilidades.CadastrarAsync(professorId, request, cancellationToken);
        return Created($"/api/professores/{professorId}/disponibilidades/{disponibilidade.Id}", disponibilidade.Id);
    }

    [HttpGet("{professorId:guid}/disponibilidades")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyCollection<DisponibilidadeProfessorDto>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<DisponibilidadeProfessorDto>> ListarDisponibilidadesPublicas(Guid professorId, CancellationToken cancellationToken)
    {
        return await _disponibilidades.ListarPublicasAsync(professorId, cancellationToken);
    }

    [HttpGet("{professorId:guid}/horarios-livres")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyCollection<HorarioLivreProfessorDto>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<HorarioLivreProfessorDto>> ListarHorariosLivres(Guid professorId, [FromQuery] DateOnly data, CancellationToken cancellationToken)
    {
        return await _disponibilidades.ListarHorariosLivresAsync(professorId, data, cancellationToken);
    }

    [HttpGet("me/disponibilidades")]
    [Authorize(Policy = "Professor")]
    [ProducesResponseType(typeof(IReadOnlyCollection<DisponibilidadeProfessorDto>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<DisponibilidadeProfessorDto>> ListarMinhasDisponibilidades(CancellationToken cancellationToken)
    {
        return await _disponibilidades.ListarMinhasAsync(User.GetUserId(), cancellationToken);
    }

    [HttpPost("me/disponibilidades")]
    [Authorize(Policy = "Professor")]
    [ProducesResponseType(typeof(DisponibilidadeProfessorDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<DisponibilidadeProfessorDto>> CadastrarMinhaDisponibilidade(CadastrarDisponibilidadeProfessorRequest request, CancellationToken cancellationToken)
    {
        var disponibilidade = await _disponibilidades.CadastrarAsync(User.GetUserId(), request, cancellationToken);
        return Created($"/api/professores/me/disponibilidades/{disponibilidade.Id}", disponibilidade);
    }

    [HttpPut("me/disponibilidades/{disponibilidadeId:guid}")]
    [Authorize(Policy = "Professor")]
    [ProducesResponseType(typeof(DisponibilidadeProfessorDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DisponibilidadeProfessorDto>> AtualizarMinhaDisponibilidade(Guid disponibilidadeId, AtualizarDisponibilidadeProfessorRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _disponibilidades.AtualizarAsync(User.GetUserId(), disponibilidadeId, request, cancellationToken));
    }

    [HttpDelete("me/disponibilidades/{disponibilidadeId:guid}")]
    [Authorize(Policy = "Professor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoverMinhaDisponibilidade(Guid disponibilidadeId, CancellationToken cancellationToken)
    {
        await _disponibilidades.RemoverAsync(User.GetUserId(), disponibilidadeId, cancellationToken);
        return NoContent();
    }

    [HttpPut("me/mentoria")]
    [Authorize(Policy = "Professor")]
    [ProducesResponseType(typeof(ProfessorDetalhesDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProfessorDetalhesDto>> ConfigurarMinhaMentoria(ConfigurarMentoriaProfessorRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _service.ConfigurarMentoriaAsync(User.GetUserId(), request, cancellationToken));
    }
}
