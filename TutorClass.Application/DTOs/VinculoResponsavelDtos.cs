using TutorClass.Domain.Users;

namespace TutorClass.Application.DTOs;

public sealed record SolicitarVinculoRequest(string NomeAluno, string? Parentesco);

public sealed record AprovarVinculoRequest(Guid AlunoId);

public sealed record RecusarVinculoRequest(string? Motivo);

public sealed record SolicitacaoVinculoResponse(
    Guid Id,
    string NomeAlunoInformado,
    string Parentesco,
    StatusSolicitacaoVinculo Status,
    string? Motivo,
    DateTime CreatedAtUtc,
    DateTime? AnalisadaEmUtc);

public sealed record CandidatoVinculoResponse(
    Guid Id,
    string Nome,
    string Email,
    DateOnly? DataNascimento);

public sealed record SolicitacaoVinculoAdminResponse(
    Guid Id,
    Guid ResponsavelId,
    string ResponsavelNome,
    string ResponsavelEmail,
    string NomeAlunoInformado,
    string Parentesco,
    StatusSolicitacaoVinculo Status,
    string? Motivo,
    Guid? AlunoId,
    string? AlunoNome,
    DateTime CreatedAtUtc,
    DateTime? AnalisadaEmUtc,
    IReadOnlyCollection<CandidatoVinculoResponse> Candidatos);
