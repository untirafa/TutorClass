using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorClass.API.Security;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;

namespace TutorClass.API.Controllers;

[ApiController]
[Route("api/responsavel")]
[Authorize(Policy = "Responsavel")]
public sealed class ResponsavelController : ControllerBase
{
    private readonly ResponsavelAlunoService _responsaveis;
    private readonly AulaService _aulas;
    private readonly PlanoAlunoService _planos;
    private readonly FinanceiroService _financeiro;
    private readonly MentoriaChatService _chat;

    public ResponsavelController(
        ResponsavelAlunoService responsaveis,
        AulaService aulas,
        PlanoAlunoService planos,
        FinanceiroService financeiro,
        MentoriaChatService chat)
    {
        _responsaveis = responsaveis;
        _aulas = aulas;
        _planos = planos;
        _financeiro = financeiro;
        _chat = chat;
    }

    [HttpGet("alunos")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ResponsavelAlunoResumoResponse>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<ResponsavelAlunoResumoResponse>> ListarAlunos(CancellationToken cancellationToken)
    {
        return await _responsaveis.ListarAlunosVinculadosAsync(User.GetUserId(), cancellationToken);
    }

    [HttpGet("alunos/{alunoId:guid}/aulas")]
    [ProducesResponseType(typeof(IReadOnlyCollection<AulaResumoResponse>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<AulaResumoResponse>> ListarAulas(Guid alunoId, CancellationToken cancellationToken)
    {
        return await _responsaveis.ListarAulasAsync(User.GetUserId(), alunoId, cancellationToken);
    }

    [HttpPost("alunos/{alunoId:guid}/aulas")]
    [ProducesResponseType(typeof(AulaDetalhesResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<AulaDetalhesResponse>> SolicitarAula(Guid alunoId, SolicitarAulaRequest request, CancellationToken cancellationToken)
    {
        await _responsaveis.ValidarVinculoAsync(User.GetUserId(), alunoId, cancellationToken);
        var aula = await _aulas.SolicitarParaAlunoAsync(alunoId, User.GetUserId(), request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, aula);
    }

    [HttpPut("alunos/{alunoId:guid}/aulas/{aulaId:guid}/reagendar")]
    [ProducesResponseType(typeof(AulaDetalhesResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AulaDetalhesResponse>> ReagendarAula(Guid alunoId, Guid aulaId, ReagendarAulaRequest request, CancellationToken cancellationToken)
    {
        _responsaveis.ValidarAulaDoAlunoVinculado(User.GetUserId(), alunoId, aulaId);
        return Ok(await _aulas.ReagendarAsync(aulaId, User.GetUserId(), request, cancellationToken));
    }

    [HttpPost("alunos/{alunoId:guid}/aulas/{aulaId:guid}/cancelar")]
    [ProducesResponseType(typeof(AulaDetalhesResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AulaDetalhesResponse>> CancelarAula(Guid alunoId, Guid aulaId, CancelarAulaRequest request, CancellationToken cancellationToken)
    {
        _responsaveis.ValidarAulaDoAlunoVinculado(User.GetUserId(), alunoId, aulaId);
        return Ok(await _aulas.CancelarAsync(aulaId, User.GetUserId(), request.Motivo, cancellationToken));
    }

    [HttpGet("alunos/{alunoId:guid}/plano")]
    [ProducesResponseType(typeof(PlanoAlunoStatusResponse), StatusCodes.Status200OK)]
    public async Task<PlanoAlunoStatusResponse> ConsultarPlano(Guid alunoId, CancellationToken cancellationToken)
    {
        return await _planos.ObterStatusParaUsuarioAsync(User.GetUserId(), alunoId, cancellationToken);
    }

    [HttpPost("alunos/{alunoId:guid}/assinatura")]
    [ProducesResponseType(typeof(AssinaturaAlunoResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<AssinaturaAlunoResponse>> AssinarPlano(Guid alunoId, ResponsavelAssinarPlanoRequest request, CancellationToken cancellationToken)
    {
        await _responsaveis.ValidarVinculoAsync(User.GetUserId(), alunoId, cancellationToken);
        var assinatura = await _planos.AssinarParaBeneficiarioAsync(
            User.GetUserId(),
            alunoId,
            new AssinarPlanoRequest(request.Plano, alunoId),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, assinatura);
    }

    [HttpGet("alunos/{alunoId:guid}/financeiro/historico")]
    [ProducesResponseType(typeof(IReadOnlyCollection<FinanceiroAlunoItemResponse>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<FinanceiroAlunoItemResponse>> HistoricoFinanceiro(Guid alunoId, CancellationToken cancellationToken)
    {
        return await _financeiro.HistoricoAlunoAsync(User.GetUserId(), alunoId, cancellationToken);
    }

    [HttpPost("alunos/{alunoId:guid}/aulas/{aulaId:guid}/pagar")]
    [ProducesResponseType(typeof(PagamentoAulaResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<PagamentoAulaResponse>> PagarAula(Guid alunoId, Guid aulaId, ResponsavelPagarAulaRequest request, CancellationToken cancellationToken)
    {
        _responsaveis.ValidarAulaDoAlunoVinculado(User.GetUserId(), alunoId, aulaId);
        var pagamento = await _financeiro.PagarAulaAsync(aulaId, User.GetUserId(), request.FormaPagamento, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, pagamento);
    }

    [HttpGet("alunos/{alunoId:guid}/mentoria")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ResponsavelMentoriaResumoResponse>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<ResponsavelMentoriaResumoResponse>> ListarMentorias(Guid alunoId, CancellationToken cancellationToken)
    {
        return await _responsaveis.ListarMentoriasAsync(User.GetUserId(), alunoId, cancellationToken);
    }

    [HttpGet("alunos/{alunoId:guid}/mentoria/acompanhamentos")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ResponsavelAcompanhamentoMentoriaResumoResponse>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<ResponsavelAcompanhamentoMentoriaResumoResponse>> ListarAcompanhamentos(Guid alunoId, CancellationToken cancellationToken)
    {
        return await _responsaveis.ListarAcompanhamentosResumidosAsync(User.GetUserId(), alunoId, cancellationToken);
    }

    [HttpGet("alunos/{alunoId:guid}/mentorias/{vinculoMentoriaId:guid}/chat/mensagens")]
    [ProducesResponseType(typeof(MensagensMentoriaPageResponse), StatusCodes.Status200OK)]
    public async Task<MensagensMentoriaPageResponse> ListarMensagensChat(
        Guid alunoId,
        Guid vinculoMentoriaId,
        [FromQuery] int? limit,
        [FromQuery] DateTimeOffset? before,
        CancellationToken cancellationToken)
    {
        _responsaveis.ValidarMentoriaDoAlunoVinculado(User.GetUserId(), alunoId, vinculoMentoriaId);
        return await _chat.ListarMensagensParaResponsavelAsync(vinculoMentoriaId, User.GetUserId(), limit, before?.UtcDateTime, cancellationToken);
    }

    [HttpGet("alunos/{alunoId:guid}/aulas/{aulaId:guid}/gravacao/resumo")]
    [ProducesResponseType(typeof(ResponsavelGravacaoAulaResumoResponse), StatusCodes.Status200OK)]
    public async Task<ResponsavelGravacaoAulaResumoResponse> ObterResumoGravacao(Guid alunoId, Guid aulaId, CancellationToken cancellationToken)
    {
        return await _responsaveis.ObterResumoGravacaoAsync(User.GetUserId(), alunoId, aulaId, cancellationToken);
    }
}
