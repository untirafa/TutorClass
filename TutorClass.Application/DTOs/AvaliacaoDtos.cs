namespace TutorClass.Application.DTOs;

public sealed record CriarAvaliacaoProfessorRequest(int Nota, string? Comentario);

public sealed record AvaliacaoProfessorResponse(
    Guid Id,
    Guid AulaId,
    Guid AlunoId,
    string Aluno,
    Guid ProfessorId,
    int Nota,
    string? Comentario,
    DateTime CreatedAtUtc);
