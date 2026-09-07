using TutorClass.Domain.Financial;
using TutorClass.Domain.Mentoring;
using TutorClass.Domain.Plans;

namespace TutorClass.Application.DTOs;

public sealed record ResponsavelAlunoResumoResponse(
    Guid Id,
    string Nome,
    string Email,
    DateOnly? DataNascimento,
    string Parentesco);

public sealed record ResponsavelMentoriaResumoResponse(
    Guid Id,
    Guid AlunoId,
    Guid ProfessorId,
    string ProfessorNome,
    int Slot,
    bool Ativo,
    DateTime DataInicioUtc,
    DateTime? DataFimUtc,
    string? MotivoEncerramento);

public sealed record ResponsavelAcompanhamentoMentoriaResumoResponse(
    Guid Id,
    Guid AulaId,
    Guid ProfessorId,
    string ProfessorNome,
    Guid VinculoMentoriaId,
    string MateriaNome,
    DateTime DataAulaUtc,
    StatusAcompanhamentoMentoria Status,
    DateTime CreatedAtUtc,
    DateTime? DataConclusaoUtc);

public sealed record ResponsavelGravacaoAulaResumoResponse(
    Guid AulaId,
    bool AulaConcluida,
    bool ExisteGravacao,
    bool GravacaoDisponivel,
    string? Titulo,
    DateTime? DataDisponibilizacaoUtc);

public sealed record ResponsavelAssinarPlanoRequest(PlanoAluno Plano);

public sealed record ResponsavelPagarAulaRequest(FormaPagamento FormaPagamento);
