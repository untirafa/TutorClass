using TutorClass.Application.Abstractions;
using TutorClass.Application.DTOs;
using TutorClass.Domain.Common;
using TutorClass.Domain.Teachers;

namespace TutorClass.Application.Services;

public sealed class CandidaturaProfessorService
{
    private readonly IApplicationDbContext _db;

    public CandidaturaProfessorService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> EnviarAsync(Guid professorId, EnviarCandidaturaProfessorRequest request, CancellationToken cancellationToken)
    {
        var professor = await _db.FindAsync<Professor>(professorId, cancellationToken)
            ?? throw new DomainException("Professor nao encontrado.");

        // Requests use separate contexts, so the navigation collection may not be loaded.
        if (_db.CandidaturasProfessor.Any(c => c.ProfessorId == professorId &&
            (c.Status == StatusCandidaturaProfessor.Pendente ||
             c.Status == StatusCandidaturaProfessor.EmAnalise ||
             c.Status == StatusCandidaturaProfessor.AguardandoDocumentacao)))
        {
            throw new DomainException("Professor ja possui uma candidatura ativa.");
        }

        var candidatura = professor.SolicitarCandidatura(request.Mensagem);
        _db.AddEntity(candidatura);
        await _db.SaveChangesAsync(cancellationToken);
        return candidatura.Id;
    }

    public Task<IReadOnlyCollection<CandidaturaProfessorDto>> ListarMinhasAsync(Guid professorId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var candidaturas = _db.CandidaturasProfessor
            .Where(c => c.ProfessorId == professorId)
            .OrderByDescending(c => c.CreatedAtUtc)
            .ThenBy(c => c.Id)
            .Select(c => new CandidaturaProfessorDto(c.Id, c.ProfessorId, c.Status,
                c.Mensagem, c.Motivo, c.AdministradorAnaliseId, c.DataAnaliseUtc))
            .ToArray();
        return Task.FromResult<IReadOnlyCollection<CandidaturaProfessorDto>>(candidaturas);
    }

    public Task<IReadOnlyCollection<CandidaturaProfessorAdminDto>> ListarParaAdministracaoAsync(Guid administradorId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureAdministradorExists(administradorId);
        var candidaturas = (from candidatura in _db.CandidaturasProfessor
                            join professor in _db.Professores on candidatura.ProfessorId equals professor.Id
                            orderby candidatura.CreatedAtUtc descending, candidatura.Id
                            select new CandidaturaProfessorAdminDto(candidatura.Id, professor.Id,
                                professor.Nome, professor.Email, candidatura.Status, candidatura.Mensagem,
                                candidatura.Motivo, candidatura.AdministradorAnaliseId,
                                candidatura.DataAnaliseUtc, candidatura.CreatedAtUtc)).ToArray();
        return Task.FromResult<IReadOnlyCollection<CandidaturaProfessorAdminDto>>(candidaturas);
    }

    public Task<CandidaturaProfessorDto?> BuscarAsync(Guid candidaturaId, Guid usuarioId, bool administrador, CancellationToken cancellationToken)
    {
        var candidatura = _db.CandidaturasProfessor.SingleOrDefault(c => c.Id == candidaturaId);
        if (candidatura is null)
        {
            return Task.FromResult<CandidaturaProfessorDto?>(null);
        }

        if (!administrador && candidatura.ProfessorId != usuarioId)
        {
            throw new UnauthorizedAccessException("Somente o professor proprietario ou um administrador podem consultar esta candidatura.");
        }

        var dto = new CandidaturaProfessorDto(
            candidatura.Id,
            candidatura.ProfessorId,
            candidatura.Status,
            candidatura.Mensagem,
            candidatura.Motivo,
            candidatura.AdministradorAnaliseId,
            candidatura.DataAnaliseUtc);

        return Task.FromResult<CandidaturaProfessorDto?>(dto);
    }

    public async Task IniciarAnaliseAsync(Guid candidaturaId, Guid administradorId, CancellationToken cancellationToken)
    {
        EnsureAdministradorExists(administradorId);

        var candidatura = await _db.FindAsync<CandidaturaProfessor>(candidaturaId, cancellationToken)
            ?? throw new DomainException("Candidatura nao encontrada.");

        candidatura.IniciarAnalise(administradorId);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AprovarAsync(Guid candidaturaId, Guid administradorId, CancellationToken cancellationToken)
    {
        EnsureAdministradorExists(administradorId);

        var candidatura = await _db.FindAsync<CandidaturaProfessor>(candidaturaId, cancellationToken)
            ?? throw new DomainException("Candidatura nao encontrada.");

        var professor = await _db.FindAsync<Professor>(candidatura.ProfessorId, cancellationToken)
            ?? throw new DomainException("Professor nao encontrado.");

        var possuiMateria = _db.ProfessoresMaterias.Any(pm => pm.ProfessorId == professor.Id);
        var possuiQualificacaoVerificada = _db.QualificacoesProfessor.Any(q =>
            q.ProfessorId == professor.Id &&
            q.StatusVerificacao == StatusVerificacaoQualificacao.Verificada);

        professor.ValidarElegibilidadeParaAprovacao(possuiMateria, possuiQualificacaoVerificada);
        candidatura.Aprovar(administradorId);
        professor.Aprovar();
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ReprovarAsync(Guid candidaturaId, DecidirCandidaturaProfessorRequest request, CancellationToken cancellationToken)
    {
        EnsureAdministradorExists(request.AdministradorId);

        var candidatura = await _db.FindAsync<CandidaturaProfessor>(candidaturaId, cancellationToken)
            ?? throw new DomainException("Candidatura nao encontrada.");

        var professor = await _db.FindAsync<Professor>(candidatura.ProfessorId, cancellationToken)
            ?? throw new DomainException("Professor nao encontrado.");

        candidatura.Reprovar(request.AdministradorId, request.Motivo ?? string.Empty);
        professor.Reprovar();
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SolicitarComplementacaoAsync(Guid candidaturaId, DecidirCandidaturaProfessorRequest request, CancellationToken cancellationToken)
    {
        EnsureAdministradorExists(request.AdministradorId);

        var candidatura = await _db.FindAsync<CandidaturaProfessor>(candidaturaId, cancellationToken)
            ?? throw new DomainException("Candidatura nao encontrada.");

        candidatura.SolicitarComplementacao(request.AdministradorId, request.Motivo ?? string.Empty);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private void EnsureAdministradorExists(Guid administradorId)
    {
        if (!_db.Administradores.Any(a => a.Id == administradorId && a.Ativo))
        {
            throw new DomainException("Administrador nao encontrado ou inativo.");
        }
    }
}
