using TutorClass.Domain.Plans;

namespace TutorClass.Application.DTOs;

public sealed record AssinarPlanoRequest(PlanoAluno Plano, Guid? BeneficiarioAlunoId = null);

public sealed record AssinaturaAlunoResponse(
    Guid Id,
    Guid AlunoId,
    Guid PagadorUsuarioId,
    PlanoAluno Plano,
    bool Ativa,
    decimal ValorMensal,
    decimal ValorCobradoPrimeiroMes,
    bool UsouPromocaoPrimeiraAssinatura,
    DateTime DataInicioUtc,
    DateTime? DataFimUtc,
    DateTime? DataCancelamentoUtc);

public sealed record PlanoAlunoStatusResponse(
    Guid AlunoId,
    PlanoAluno PlanoAtual,
    bool AssinaturaAtiva,
    bool PossuiIaEducacional,
    int? LimiteMensagensIaDia,
    bool PossuiMentoria,
    bool PossuiGravacoesElegiveis,
    bool JaUsouPromocaoPrimeiraAssinatura);

public sealed record UsoIaAlunoResponse(
    PlanoAluno PlanoAtual,
    bool Permitido,
    int QuantidadeUsadaHoje,
    int? LimiteDiario,
    string? Mensagem);
