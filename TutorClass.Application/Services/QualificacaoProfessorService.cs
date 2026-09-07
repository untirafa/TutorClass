using TutorClass.Application.Abstractions;
using TutorClass.Application.DTOs;
using TutorClass.Domain.Common;
using TutorClass.Domain.Teachers;

namespace TutorClass.Application.Services;

public sealed class QualificacaoProfessorService
{
    private readonly IApplicationDbContext _db;

    public QualificacaoProfessorService(IApplicationDbContext db)
    {
        _db = db;
    }

    public Task<IReadOnlyCollection<QualificacaoProfessorDto>> ListarPorProfessorAsync(Guid professorId, Guid usuarioId, bool administrador, CancellationToken cancellationToken)
    {
        if (!_db.Professores.Any(p => p.Id == professorId && p.Ativo))
        {
            throw new DomainException("Professor nao encontrado ou inativo.");
        }

        if (!administrador && professorId != usuarioId)
        {
            throw new UnauthorizedAccessException("Somente o professor proprietario ou um administrador podem consultar os comprovantes das qualificacoes.");
        }

        var qualificacoes = _db.QualificacoesProfessor
            .Where(q => q.ProfessorId == professorId)
            .OrderBy(q => q.Titulo)
            .Select(q => new QualificacaoProfessorDto(
                q.Id,
                q.ProfessorId,
                q.Tipo,
                q.Titulo,
                q.Instituicao,
                q.Descricao,
                q.DataConclusao,
                q.DocumentoComprovacao,
                q.StatusVerificacao,
                q.DataVerificacaoUtc))
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<QualificacaoProfessorDto>>(qualificacoes);
    }

    public async Task<QualificacaoProfessorDto> VerificarAsync(Guid qualificacaoId, Guid administradorId, CancellationToken cancellationToken)
    {
        EnsureAdministradorExists(administradorId);

        var qualificacao = await _db.FindAsync<QualificacaoProfessor>(qualificacaoId, cancellationToken)
            ?? throw new DomainException("Qualificacao nao encontrada.");

        qualificacao.Verificar();
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(qualificacao);
    }

    public async Task<QualificacaoProfessorDto> RejeitarAsync(Guid qualificacaoId, Guid administradorId, CancellationToken cancellationToken)
    {
        EnsureAdministradorExists(administradorId);

        var qualificacao = await _db.FindAsync<QualificacaoProfessor>(qualificacaoId, cancellationToken)
            ?? throw new DomainException("Qualificacao nao encontrada.");

        qualificacao.Rejeitar();
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(qualificacao);
    }

    private void EnsureAdministradorExists(Guid administradorId)
    {
        if (!_db.Administradores.Any(a => a.Id == administradorId && a.Ativo))
        {
            throw new DomainException("Administrador nao encontrado ou inativo.");
        }
    }

    private static QualificacaoProfessorDto ToDto(QualificacaoProfessor qualificacao)
    {
        return new QualificacaoProfessorDto(
            qualificacao.Id,
            qualificacao.ProfessorId,
            qualificacao.Tipo,
            qualificacao.Titulo,
            qualificacao.Instituicao,
            qualificacao.Descricao,
            qualificacao.DataConclusao,
            qualificacao.DocumentoComprovacao,
            qualificacao.StatusVerificacao,
            qualificacao.DataVerificacaoUtc);
    }
}
