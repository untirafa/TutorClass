using TutorClass.Domain.Financial;
using TutorClass.Domain.Plans;

namespace TutorClass.Application.DTOs;

public sealed record PagamentoAulaResponse(
    Guid Id,
    Guid AulaId,
    Guid AlunoId,
    string AlunoNome,
    Guid PagadorUsuarioId,
    string PagadorNome,
    Guid ProfessorId,
    string ProfessorNome,
    decimal ValorBruto,
    decimal PercentualComissao,
    decimal ValorComissao,
    decimal ValorLiquidoProfessor,
    FormaPagamento FormaPagamento,
    StatusPagamentoFinanceiro Status,
    DateTime CreatedAtUtc,
    DateTime? DataPagamentoUtc);

public sealed record PagarAulaRequest(FormaPagamento FormaPagamento);

public sealed record GerarCobrancaAssinaturaRequest(
    int AnoCompetencia,
    int MesCompetencia,
    FormaPagamento FormaPagamento);

public sealed record CobrancaAssinaturaResponse(
    Guid Id,
    Guid AssinaturaId,
    Guid BeneficiarioAlunoId,
    Guid PagadorUsuarioId,
    string PagadorNome,
    PlanoAluno Plano,
    int AnoCompetencia,
    int MesCompetencia,
    decimal Valor,
    FormaPagamento FormaPagamento,
    StatusPagamentoFinanceiro Status,
    DateTime CreatedAtUtc,
    DateTime? PaidAtUtc);

public sealed record FinanceiroAlunoItemResponse(
    string Tipo,
    Guid Id,
    Guid BeneficiarioAlunoId,
    Guid PagadorUsuarioId,
    decimal Valor,
    string Descricao,
    DateTime DataUtc,
    string Status);

public sealed record FinanceiroProfessorItemResponse(
    string Tipo,
    Guid Id,
    Guid? AulaId,
    Guid AlunoId,
    string AlunoNome,
    decimal ValorBruto,
    decimal ValorComissao,
    decimal ValorLiquidoProfessor,
    decimal ValorAdicionalMentoria,
    DateTime DataUtc,
    string Status);

public sealed record ResumoFinanceiroProfessorResponse(
    Guid ProfessorId,
    int Ano,
    int Mes,
    decimal ValorBrutoAulas,
    decimal ComissoesTutorClass,
    decimal ValorLiquidoAulas,
    decimal AdicionaisMentoria,
    decimal TotalPrevisto,
    IReadOnlyCollection<FinanceiroProfessorItemResponse> Itens);

public sealed record DemonstrativoComissaoAulaResponse(
    decimal ValorBruto,
    decimal PercentualComissao,
    decimal ValorComissao,
    decimal ValorLiquidoProfessor);
