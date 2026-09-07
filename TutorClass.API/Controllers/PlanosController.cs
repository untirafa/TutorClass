using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorClass.API.Security;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;

namespace TutorClass.API.Controllers;

[ApiController]
[Route("api/planos")]
[Authorize]
public sealed class PlanosController : ControllerBase
{
    private readonly PlanoAlunoService _service;

    public PlanosController(PlanoAlunoService service)
    {
        _service = service;
    }

    [HttpGet("me")]
    [Authorize(Policy = "Aluno")]
    [ProducesResponseType(typeof(PlanoAlunoStatusResponse), StatusCodes.Status200OK)]
    public async Task<PlanoAlunoStatusResponse> MeuPlano(CancellationToken cancellationToken)
    {
        return await _service.ObterStatusAsync(User.GetUserId(), cancellationToken);
    }

    [HttpGet("alunos/{alunoId:guid}")]
    [ProducesResponseType(typeof(PlanoAlunoStatusResponse), StatusCodes.Status200OK)]
    public async Task<PlanoAlunoStatusResponse> PlanoDoAluno(Guid alunoId, CancellationToken cancellationToken)
    {
        return await _service.ObterStatusParaUsuarioAsync(User.GetUserId(), alunoId, cancellationToken);
    }

    [HttpPost("me/assinatura")]
    [Authorize(Policy = "Aluno")]
    [ProducesResponseType(typeof(AssinaturaAlunoResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<AssinaturaAlunoResponse>> Assinar(AssinarPlanoRequest request, CancellationToken cancellationToken)
    {
        var assinatura = await _service.AssinarAsync(User.GetUserId(), request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, assinatura);
    }

    [HttpPost("assinatura")]
    [ProducesResponseType(typeof(AssinaturaAlunoResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<AssinaturaAlunoResponse>> AssinarParaBeneficiario(AssinarPlanoRequest request, CancellationToken cancellationToken)
    {
        var beneficiarioAlunoId = request.BeneficiarioAlunoId ?? User.GetUserId();
        var assinatura = await _service.AssinarParaBeneficiarioAsync(User.GetUserId(), beneficiarioAlunoId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, assinatura);
    }

    [HttpPost("me/cancelar")]
    [Authorize(Policy = "Aluno")]
    [ProducesResponseType(typeof(PlanoAlunoStatusResponse), StatusCodes.Status200OK)]
    public async Task<PlanoAlunoStatusResponse> Cancelar(CancellationToken cancellationToken)
    {
        return await _service.CancelarAtualAsync(User.GetUserId(), cancellationToken);
    }

    [HttpGet("me/ia")]
    [Authorize(Policy = "Aluno")]
    [ProducesResponseType(typeof(UsoIaAlunoResponse), StatusCodes.Status200OK)]
    public async Task<UsoIaAlunoResponse> PodeUsarIa(CancellationToken cancellationToken)
    {
        return await _service.PodeUsarIaAsync(User.GetUserId(), cancellationToken);
    }

    [HttpPost("me/ia/uso")]
    [Authorize(Policy = "Aluno")]
    [ProducesResponseType(typeof(UsoIaAlunoResponse), StatusCodes.Status200OK)]
    public async Task<UsoIaAlunoResponse> RegistrarUsoIa(CancellationToken cancellationToken)
    {
        return await _service.RegistrarUsoIaAsync(User.GetUserId(), cancellationToken);
    }
}
