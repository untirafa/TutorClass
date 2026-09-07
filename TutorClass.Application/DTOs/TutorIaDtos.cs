using TutorClass.Domain.Plans;

namespace TutorClass.Application.DTOs;

public sealed record TutorIaStatusResponse(
    string Nome,
    bool PossuiAcesso,
    PlanoAluno Plano,
    int? LimiteDiario,
    int UsadasHoje,
    int? RestantesHoje,
    bool LimiteAtingido,
    string? Motivo,
    bool PodeConhecerPlanos,
    string Mensagem);

public sealed record TutorIaChatRequest(string Mensagem);

public sealed record TutorIaChatResponse(
    string Nome,
    bool PossuiAcesso,
    PlanoAluno Plano,
    string? Motivo,
    string Mensagem,
    string? Resposta,
    int? LimiteDiario,
    int UsadasHoje,
    int? RestantesHoje,
    bool LimiteAtingido,
    bool PodeConhecerPlanos,
    bool PodeConhecerMentoria);

public sealed record TutorIaProviderRequest(
    Guid AlunoId,
    PlanoAluno Plano,
    string Mensagem,
    DateOnly DataReferencia);
