using TutorClass.Domain.Teachers;

namespace TutorClass.Application.DTOs;

public sealed record ProfessorResumoDto(
    Guid Id,
    string Nome,
    string? Biografia,
    decimal? PrecoHora,
    NivelReputacaoProfessor NivelReputacao,
    decimal? MediaAvaliacoes,
    int QuantidadeAvaliacoes,
    int AulasConcluidas,
    bool OfereceMentoria,
    int LimiteMentorados,
    string TextoAvaliacoes,
    IReadOnlyCollection<string> Materias);

public sealed record ProfessorDetalhesDto(
    Guid Id,
    string Nome,
    string? Biografia,
    decimal? PrecoHora,
    NivelReputacaoProfessor NivelReputacao,
    decimal? MediaAvaliacoes,
    int QuantidadeAvaliacoes,
    int AulasConcluidas,
    bool OfereceMentoria,
    int LimiteMentorados,
    string TextoAvaliacoes,
    IReadOnlyCollection<string> Materias,
    IReadOnlyCollection<QualificacaoProfessorDto> Qualificacoes,
    IReadOnlyCollection<DisponibilidadeProfessorDto> Disponibilidades);

public sealed record CriarProfessorRequest(string Nome, string Email, string Senha, string Biografia, decimal PrecoHora);
public sealed record VincularMateriaProfessorRequest(Guid MateriaId);

public sealed record AtualizarPerfilProfessorRequest(string Biografia, decimal PrecoHora);

public sealed record ProfessorPerfilPrivadoDto(
    Guid Id,
    string Nome,
    string Email,
    StatusProfessor Status,
    string? Biografia,
    decimal? PrecoHora,
    bool OfereceMentoria,
    int LimiteMentorados,
    IReadOnlyCollection<MateriaDto> Materias,
    IReadOnlyCollection<QualificacaoProfessorDto> Qualificacoes,
    IReadOnlyCollection<DisponibilidadeProfessorDto> Disponibilidades);
