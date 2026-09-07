using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorClass.API.Security;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;

namespace TutorClass.API.Controllers;

[ApiController]
[Route("api/financeiro")]
[Authorize]
public sealed class FinanceiroController : ControllerBase
{
    private readonly FinanceiroService _service;

    public FinanceiroController(FinanceiroService service)
    {
        _service = service;
    }

    [HttpGet("aulas/comissao")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(DemonstrativoComissaoAulaResponse), StatusCodes.Status200OK)]
    public ActionResult<DemonstrativoComissaoAulaResponse> CalcularComissao([FromQuery] decimal valorBruto)
    {
        return Ok(_service.CalcularComissao(valorBruto));
    }

    [HttpPost("aulas/{aulaId:guid}/pagar")]
    [ProducesResponseType(typeof(PagamentoAulaResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<PagamentoAulaResponse>> PagarAula(Guid aulaId, [FromBody] PagarAulaRequest? request, CancellationToken cancellationToken)
    {
        var pagamento = await _service.PagarAulaAsync(aulaId, User.GetUserId(), request?.FormaPagamento ?? TutorClass.Domain.Financial.FormaPagamento.Pix, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, pagamento);
    }

    [HttpPost("assinaturas/{assinaturaId:guid}/cobrancas")]
    [ProducesResponseType(typeof(CobrancaAssinaturaResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<CobrancaAssinaturaResponse>> GerarCobrancaAssinatura(Guid assinaturaId, GerarCobrancaAssinaturaRequest request, CancellationToken cancellationToken)
    {
        var pagamento = await _service.GerarCobrancaAssinaturaAsync(assinaturaId, User.GetUserId(), request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, pagamento);
    }

    [HttpGet("me/historico")]
    [Authorize(Policy = "Aluno")]
    [ProducesResponseType(typeof(IReadOnlyCollection<FinanceiroAlunoItemResponse>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<FinanceiroAlunoItemResponse>> MeuHistorico(CancellationToken cancellationToken)
    {
        return await _service.HistoricoAlunoAsync(User.GetUserId(), User.GetUserId(), cancellationToken);
    }

    [HttpGet("alunos/{alunoId:guid}/historico")]
    [ProducesResponseType(typeof(IReadOnlyCollection<FinanceiroAlunoItemResponse>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<FinanceiroAlunoItemResponse>> HistoricoAluno(Guid alunoId, CancellationToken cancellationToken)
    {
        return await _service.HistoricoAlunoAsync(User.GetUserId(), alunoId, cancellationToken);
    }

    [HttpGet("professores/me/resumo")]
    [Authorize(Policy = "Professor")]
    [ProducesResponseType(typeof(ResumoFinanceiroProfessorResponse), StatusCodes.Status200OK)]
    public async Task<ResumoFinanceiroProfessorResponse> MeuResumoProfessor([FromQuery] int ano, [FromQuery] int mes, CancellationToken cancellationToken)
    {
        return await _service.ResumoProfessorAsync(User.GetUserId(), ano, mes, cancellationToken);
    }
}
