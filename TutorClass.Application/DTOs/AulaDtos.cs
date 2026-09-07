using TutorClass.Domain.Lessons;

namespace TutorClass.Application.DTOs;

public sealed record SolicitarAulaRequest(
    Guid ProfessorId,
    Guid MateriaId,
    DateTime Inicio,
    int DuracaoMinutos);

public sealed record CancelarAulaRequest(string? Motivo);

public sealed record ReagendarAulaRequest(DateTime Inicio, int? DuracaoMinutos);

public sealed record AulaReagendamentoResponse(
    Guid Id,
    Guid AulaId,
    DateTime InicioAnterior,
    DateTime FimAnterior,
    DateTime InicioNovo,
    DateTime FimNovo,
    Guid SolicitadoPorUsuarioId,
    DateTime CreatedAtUtc);

public sealed record ParticipanteAulaDto(Guid Id, string Nome);

public sealed record MateriaAulaDto(Guid Id, string Nome);

public sealed record AulaDetalhesResponse(
    Guid Id,
    ParticipanteAulaDto Professor,
    ParticipanteAulaDto Aluno,
    Guid SolicitadaPorUsuarioId,
    MateriaAulaDto Materia,
    DateTime Inicio,
    int DuracaoMinutos,
    decimal PrecoAcordado,
    StatusAula Status,
    DateTime? ConfirmadaEmUtc,
    DateTime? CanceladaEmUtc,
    DateTime? ConcluidaEmUtc,
    string? MotivoCancelamento,
    bool AulaConcluida = false,
    bool RequerAcompanhamentoMentoria = false,
    Guid? AcompanhamentoMentoriaId = null);

public sealed record AulaResumoResponse(
    Guid Id,
    string Professor,
    string Aluno,
    Guid SolicitadaPorUsuarioId,
    string Materia,
    DateTime Inicio,
    int DuracaoMinutos,
    decimal PrecoAcordado,
    StatusAula Status);

public sealed record CriarGravacaoAulaRequest(
    string Titulo,
    string UrlVideo,
    int DuracaoSegundos,
    string? Descricao = null,
    string? MiniaturaUrl = null);

public sealed record GravacaoAulaResponse(
    Guid Id,
    Guid AulaId,
    string Titulo,
    string? Descricao,
    string UrlVideo,
    string? MiniaturaUrl,
    int DuracaoSegundos,
    DateTime? DataDisponibilizacaoUtc,
    bool Disponivel,
    string Materia,
    string Professor,
    DateTime AulaInicio);
