using TutorClass.Domain.Mentoring;

namespace TutorClass.Application.DTOs;

public sealed record ConfigurarMentoriaProfessorRequest(bool OfereceMentoria, int LimiteMentorados);

public sealed record SelecionarMentorRequest(Guid ProfessorId, int? Slot);

public sealed record TrocarMentorRequest(Guid NovoProfessorId);

public sealed record MentoriaResponse(
    Guid Id,
    Guid AlunoId,
    string AlunoNome,
    Guid ProfessorId,
    string ProfessorNome,
    int Slot,
    bool Ativo,
    DateTime DataInicioUtc,
    DateTime? DataFimUtc,
    DateTime DataUltimaTrocaUtc,
    string? MotivoEncerramento);

public sealed record AtualizarAcompanhamentoMentoriaRequest(
    string? Resumo,
    string? PontosFortes,
    string? PontosMelhorar,
    string? ProximosPassos,
    string? MetaAtual,
    string? Observacoes);

public sealed record AcompanhamentoMentoriaResponse(
    Guid Id,
    Guid AulaId,
    Guid AlunoId,
    string AlunoNome,
    Guid ProfessorId,
    string ProfessorNome,
    Guid VinculoMentoriaId,
    string MateriaNome,
    DateTime DataAulaUtc,
    string? Resumo,
    string? PontosFortes,
    string? PontosMelhorar,
    string? ProximosPassos,
    string? MetaAtual,
    string? Observacoes,
    StatusAcompanhamentoMentoria Status,
    DateTime CreatedAtUtc,
    DateTime? DataConclusaoUtc);

public sealed record ConclusaoAcompanhamentoMentoriaResponse(
    AcompanhamentoMentoriaResponse Acompanhamento,
    bool AdicionalGerado,
    decimal? ValorAdicional,
    string Mensagem);
