using TutorClass.Domain.Teachers;

namespace TutorClass.Application.DTOs;

public sealed record EnviarCandidaturaProfessorRequest(string Mensagem);
public sealed record DecidirCandidaturaProfessorRequest(Guid AdministradorId, string? Motivo);

public sealed record CandidaturaProfessorDto(
    Guid Id,
    Guid ProfessorId,
    StatusCandidaturaProfessor Status,
    string Mensagem,
    string? Motivo,
    Guid? AdministradorAnaliseId,
    DateTime? DataAnaliseUtc);

public sealed record CandidaturaProfessorAdminDto(
    Guid Id,
    Guid ProfessorId,
    string ProfessorNome,
    string ProfessorEmail,
    StatusCandidaturaProfessor Status,
    string Mensagem,
    string? Motivo,
    Guid? AdministradorAnaliseId,
    DateTime? DataAnaliseUtc,
    DateTime CreatedAtUtc);
