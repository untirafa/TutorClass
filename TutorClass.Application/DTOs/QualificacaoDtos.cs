using TutorClass.Domain.Teachers;

namespace TutorClass.Application.DTOs;

public sealed record AdicionarQualificacaoProfessorRequest(
    TipoQualificacaoProfessor Tipo,
    string Titulo,
    string? Instituicao,
    string? Descricao,
    DateOnly? DataConclusao,
    string? DocumentoComprovacao);

public sealed record QualificacaoProfessorDto(
    Guid Id,
    Guid ProfessorId,
    TipoQualificacaoProfessor Tipo,
    string Titulo,
    string? Instituicao,
    string? Descricao,
    DateOnly? DataConclusao,
    string? DocumentoComprovacao,
    StatusVerificacaoQualificacao StatusVerificacao,
    DateTime? DataVerificacaoUtc);
