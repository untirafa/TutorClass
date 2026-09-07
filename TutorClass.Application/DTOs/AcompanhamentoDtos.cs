namespace TutorClass.Application.DTOs;

public sealed record RegistrarAcompanhamentoAulaRequest(
    string Resumo,
    string? PontosFortes,
    string? PontosMelhorar,
    string? ProximosPassos);

public sealed record AcompanhamentoAulaResponse(
    Guid Id,
    Guid AulaId,
    Guid AlunoId,
    Guid ProfessorId,
    string Resumo,
    string? PontosFortes,
    string? PontosMelhorar,
    string? ProximosPassos,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
