using TutorClass.Application.Abstractions;
using TutorClass.Application.DTOs;
using TutorClass.Domain.Common;
using TutorClass.Domain.Teachers;
using TutorClass.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace TutorClass.Application.Services;

public sealed class ProfessorService
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher<Usuario> _passwordHasher;

    public ProfessorService(IApplicationDbContext db)
        : this(db, new PasswordHasher<Usuario>())
    {
    }

    public ProfessorService(IApplicationDbContext db, IPasswordHasher<Usuario> passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<Guid> CriarProfessorAsync(CriarProfessorRequest request, CancellationToken cancellationToken)
    {
        if (_db.Usuarios.Any(u => u.Email == request.Email.Trim().ToLowerInvariant()))
        {
            throw new DomainException("Email ja cadastrado.");
        }

        if (string.IsNullOrWhiteSpace(request.Senha) || request.Senha.Length < 8)
        {
            throw new DomainException("Senha deve possuir pelo menos 8 caracteres.");
        }

        var professor = new Professor(request.Nome, request.Email, "pending");
        professor.DefinirPasswordHash(_passwordHasher.HashPassword(professor, request.Senha));
        professor.AtualizarPerfilProfissional(request.Biografia, request.PrecoHora);

        _db.AddEntity(professor);
        await _db.SaveChangesAsync(cancellationToken);
        return professor.Id;
    }

    public async Task VincularMateriaAsync(Guid professorId, VincularMateriaProfessorRequest request, CancellationToken cancellationToken)
    {
        var professor = await _db.FindAsync<Professor>(professorId, cancellationToken)
            ?? throw new DomainException("Professor nao encontrado.");

        if (!_db.Materias.Any(m => m.Id == request.MateriaId))
        {
            throw new DomainException("Materia nao encontrada.");
        }

        if (_db.ProfessoresMaterias.Any(pm => pm.ProfessorId == professorId && pm.MateriaId == request.MateriaId))
        {
            return;
        }

        var professorMateria = professor.VincularMateria(request.MateriaId);
        _db.AddEntity(professorMateria);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<IReadOnlyCollection<ProfessorResumoDto>> ListarAprovadosAsync(Guid? materiaId, CancellationToken cancellationToken)
    {
        var query = _db.Professores
            .Where(p => p.Ativo
                && p.Status == StatusProfessor.Aprovado
                && p.Candidaturas.Any(c => c.Status == StatusCandidaturaProfessor.Aprovada));

        if (materiaId is not null)
        {
            query = query.Where(p => p.Materias.Any(m => m.MateriaId == materiaId));
        }

        var professores = query
            .OrderByDescending(p => p.NivelReputacao)
            .ThenByDescending(p => p.QuantidadeAvaliacoes)
            .ToArray()
            .Select(p => new ProfessorResumoDto(
                p.Id,
                p.Nome,
                p.Biografia,
                p.PrecoHora,
                p.NivelReputacao,
                p.MediaAvaliacoes,
                p.QuantidadeAvaliacoes,
                p.AulasConcluidas,
                p.OfereceMentoria,
                p.LimiteMentorados,
                p.QuantidadeAvaliacoes == 0 ? "Novo na TutorClass - ainda sem avaliacoes" : $"{p.MediaAvaliacoes:0.0} estrelas ({p.QuantidadeAvaliacoes} avaliacoes)",
                GetMateriasProfessor(p.Id)))
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<ProfessorResumoDto>>(professores);
    }

    public Task<ProfessorDetalhesDto?> BuscarPublicoPorIdAsync(Guid professorId, CancellationToken cancellationToken)
    {
        var professor = _db.Professores
            .Where(p => p.Id == professorId
                && p.Ativo
                && p.Status == StatusProfessor.Aprovado
                && p.Candidaturas.Any(c => c.Status == StatusCandidaturaProfessor.Aprovada))
            .ToArray()
            .Select(p => new ProfessorDetalhesDto(
                p.Id,
                p.Nome,
                p.Biografia,
                p.PrecoHora,
                p.NivelReputacao,
                p.MediaAvaliacoes,
                p.QuantidadeAvaliacoes,
                p.AulasConcluidas,
                p.OfereceMentoria,
                p.LimiteMentorados,
                p.QuantidadeAvaliacoes == 0 ? "Novo na TutorClass - ainda sem avaliacoes" : $"{p.MediaAvaliacoes:0.0} estrelas ({p.QuantidadeAvaliacoes} avaliacoes)",
                GetMateriasProfessor(p.Id),
                GetQualificacoesPublicas(p.Id),
                GetDisponibilidadesProfessor(p.Id)))
            .SingleOrDefault();

        return Task.FromResult(professor);
    }

    public async Task<QualificacaoProfessorDto> AdicionarQualificacaoAsync(Guid professorId, AdicionarQualificacaoProfessorRequest request, CancellationToken cancellationToken)
    {
        var professor = await _db.FindAsync<Professor>(professorId, cancellationToken)
            ?? throw new DomainException("Professor nao encontrado.");

        var qualificacao = professor.AdicionarQualificacao(request.Tipo, request.Titulo, request.Instituicao, request.Descricao, request.DataConclusao, request.DocumentoComprovacao);
        _db.AddEntity(qualificacao);
        await _db.SaveChangesAsync(cancellationToken);
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

    public async Task<Guid> CadastrarDisponibilidadeAsync(Guid professorId, CadastrarDisponibilidadeProfessorRequest request, CancellationToken cancellationToken)
    {
        var professor = await _db.FindAsync<Professor>(professorId, cancellationToken)
            ?? throw new DomainException("Professor nao encontrado.");

        var disponibilidade = professor.CadastrarDisponibilidade(request.DiaSemana, request.Inicio, request.Fim);
        _db.AddEntity(disponibilidade);
        await _db.SaveChangesAsync(cancellationToken);
        return disponibilidade.Id;
    }

    public async Task<ProfessorDetalhesDto> ConfigurarMentoriaAsync(Guid professorId, ConfigurarMentoriaProfessorRequest request, CancellationToken cancellationToken)
    {
        var professor = await _db.FindAsync<Professor>(professorId, cancellationToken)
            ?? throw new DomainException("Professor nao encontrado.");

        professor.ConfigurarMentoria(request.OfereceMentoria, request.LimiteMentorados);
        await _db.SaveChangesAsync(cancellationToken);

        return await BuscarPublicoPorIdAsync(professorId, cancellationToken)
            ?? new ProfessorDetalhesDto(
                professor.Id,
                professor.Nome,
                professor.Biografia,
                professor.PrecoHora,
                professor.NivelReputacao,
                professor.MediaAvaliacoes,
                professor.QuantidadeAvaliacoes,
                professor.AulasConcluidas,
                professor.OfereceMentoria,
                professor.LimiteMentorados,
                professor.QuantidadeAvaliacoes == 0 ? "Novo na TutorClass - ainda sem avaliacoes" : $"{professor.MediaAvaliacoes:0.0} estrelas ({professor.QuantidadeAvaliacoes} avaliacoes)",
                [],
                [],
                []);
    }

    private IReadOnlyCollection<string> GetMateriasProfessor(Guid professorId)
    {
        var materiaIds = _db.ProfessoresMaterias
            .Where(pm => pm.ProfessorId == professorId)
            .Select(pm => pm.MateriaId)
            .ToArray();

        return _db.Materias
            .Where(m => materiaIds.Contains(m.Id))
            .OrderBy(m => m.Nome)
            .Select(m => m.Nome)
            .ToArray();
    }

    private IReadOnlyCollection<QualificacaoProfessorDto> GetQualificacoesPublicas(Guid professorId)
    {
        return _db.QualificacoesProfessor
            .Where(q => q.ProfessorId == professorId && q.StatusVerificacao == StatusVerificacaoQualificacao.Verificada)
            .OrderBy(q => q.Titulo)
            .ToArray()
            .Select(q => new QualificacaoProfessorDto(q.Id, q.ProfessorId, q.Tipo, q.Titulo, q.Instituicao, q.Descricao, q.DataConclusao, null, q.StatusVerificacao, q.DataVerificacaoUtc))
            .ToArray();
    }

    private IReadOnlyCollection<DisponibilidadeProfessorDto> GetDisponibilidadesProfessor(Guid professorId)
    {
        return _db.DisponibilidadesProfessor
            .Where(d => d.ProfessorId == professorId)
            .OrderBy(d => d.DiaSemana)
            .ThenBy(d => d.Inicio)
            .ToArray()
            .Select(d => new DisponibilidadeProfessorDto(d.Id, d.DiaSemana, d.Inicio, d.Fim))
            .ToArray();
    }
}
