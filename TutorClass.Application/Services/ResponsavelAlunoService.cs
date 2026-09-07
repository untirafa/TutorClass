using TutorClass.Application.Abstractions;
using TutorClass.Application.DTOs;
using TutorClass.Domain.Common;
using TutorClass.Domain.Lessons;
using TutorClass.Domain.Mentoring;
using TutorClass.Domain.Users;

namespace TutorClass.Application.Services;

public sealed class ResponsavelAlunoService
{
    private readonly IApplicationDbContext _db;

    public ResponsavelAlunoService(IApplicationDbContext db)
    {
        _db = db;
    }

    public Task<bool> PossuiVinculoAsync(Guid responsavelId, Guid alunoId, CancellationToken cancellationToken)
    {
        return Task.FromResult(PossuiVinculo(responsavelId, alunoId));
    }

    public bool PossuiVinculo(Guid responsavelId, Guid alunoId)
    {
        return _db.Usuarios.OfType<Responsavel>().Any(r => r.Id == responsavelId && r.Ativo) &&
            _db.Usuarios.OfType<Aluno>().Any(a => a.Id == alunoId && a.Ativo) &&
            _db.ResponsaveisAlunos.Any(ra => ra.ResponsavelId == responsavelId && ra.AlunoId == alunoId);
    }

    public Task ValidarVinculoAsync(Guid responsavelId, Guid alunoId, CancellationToken cancellationToken)
    {
        ValidarVinculo(responsavelId, alunoId);
        return Task.CompletedTask;
    }

    public void ValidarVinculo(Guid responsavelId, Guid alunoId)
    {
        if (!_db.Usuarios.OfType<Responsavel>().Any(r => r.Id == responsavelId && r.Ativo))
        {
            throw new UnauthorizedAccessException("Esta operacao e permitida somente para o perfil Responsavel.");
        }

        if (!_db.Usuarios.OfType<Aluno>().Any(a => a.Id == alunoId && a.Ativo))
        {
            throw new DomainException("Aluno nao encontrado ou inativo.");
        }

        if (!_db.ResponsaveisAlunos.Any(ra => ra.ResponsavelId == responsavelId && ra.AlunoId == alunoId))
        {
            throw new UnauthorizedAccessException("Voce nao possui vinculo com este aluno.");
        }
    }

    public Task<IReadOnlyCollection<ResponsavelAlunoResumoResponse>> ListarAlunosVinculadosAsync(Guid responsavelId, CancellationToken cancellationToken)
    {
        if (!_db.Usuarios.OfType<Responsavel>().Any(r => r.Id == responsavelId && r.Ativo))
        {
            throw new UnauthorizedAccessException("Esta operacao e permitida somente para o perfil Responsavel.");
        }

        var result = _db.ResponsaveisAlunos
            .Where(ra => ra.ResponsavelId == responsavelId)
            .Join(
                _db.Usuarios.OfType<Aluno>().Where(a => a.Ativo),
                ra => ra.AlunoId,
                aluno => aluno.Id,
                (ra, aluno) => new ResponsavelAlunoResumoResponse(
                    aluno.Id,
                    aluno.Nome,
                    aluno.Email,
                    aluno.DataNascimento,
                    ra.Parentesco))
            .ToArray()
            .OrderBy(a => a.Nome)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<ResponsavelAlunoResumoResponse>>(result);
    }

    public Task<IReadOnlyCollection<AulaResumoResponse>> ListarAulasAsync(Guid responsavelId, Guid alunoId, CancellationToken cancellationToken)
    {
        ValidarVinculo(responsavelId, alunoId);

        var aulas = _db.Aulas
            .Where(a => a.AlunoId == alunoId)
            .OrderByDescending(a => a.InicioUtc)
            .ToArray()
            .Select(ToAulaResumo)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<AulaResumoResponse>>(aulas);
    }

    public Task<IReadOnlyCollection<ResponsavelMentoriaResumoResponse>> ListarMentoriasAsync(Guid responsavelId, Guid alunoId, CancellationToken cancellationToken)
    {
        ValidarVinculo(responsavelId, alunoId);

        var mentorias = _db.MentoriasAlunoProfessor
            .Where(m => m.AlunoId == alunoId)
            .OrderByDescending(m => m.Ativo)
            .ThenBy(m => m.Slot)
            .ThenByDescending(m => m.DataInicioUtc)
            .ToArray()
            .Select(m => new ResponsavelMentoriaResumoResponse(
                m.Id,
                m.AlunoId,
                m.ProfessorId,
                GetProfessorNome(m.ProfessorId),
                m.Slot,
                m.Ativo,
                m.DataInicioUtc,
                m.DataFimUtc,
                m.MotivoEncerramento?.ToString()))
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<ResponsavelMentoriaResumoResponse>>(mentorias);
    }

    public Task<IReadOnlyCollection<ResponsavelAcompanhamentoMentoriaResumoResponse>> ListarAcompanhamentosResumidosAsync(Guid responsavelId, Guid alunoId, CancellationToken cancellationToken)
    {
        ValidarVinculo(responsavelId, alunoId);

        var acompanhamentos = _db.AcompanhamentosMentoria
            .Where(a => a.AlunoId == alunoId)
            .OrderByDescending(a => a.CreatedAtUtc)
            .ToArray()
            .Select(ToAcompanhamentoResumo)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<ResponsavelAcompanhamentoMentoriaResumoResponse>>(acompanhamentos);
    }

    public Task<ResponsavelGravacaoAulaResumoResponse> ObterResumoGravacaoAsync(Guid responsavelId, Guid alunoId, Guid aulaId, CancellationToken cancellationToken)
    {
        ValidarVinculo(responsavelId, alunoId);

        var aula = _db.Aulas.SingleOrDefault(a => a.Id == aulaId && a.AlunoId == alunoId)
            ?? throw new DomainException("Aula nao encontrada para este aluno.");

        var gravacao = _db.GravacoesAula.SingleOrDefault(g => g.AulaId == aula.Id);

        return Task.FromResult(new ResponsavelGravacaoAulaResumoResponse(
            aula.Id,
            aula.Status == StatusAula.Concluida,
            gravacao is not null,
            gravacao?.Disponivel ?? false,
            gravacao?.Titulo,
            gravacao?.DataDisponibilizacaoUtc));
    }

    public void ValidarAulaDoAlunoVinculado(Guid responsavelId, Guid alunoId, Guid aulaId)
    {
        ValidarVinculo(responsavelId, alunoId);

        if (!_db.Aulas.Any(a => a.Id == aulaId && a.AlunoId == alunoId))
        {
            throw new UnauthorizedAccessException("Esta aula nao pertence a um aluno vinculado a voce.");
        }
    }

    public void ValidarMentoriaDoAlunoVinculado(Guid responsavelId, Guid alunoId, Guid vinculoMentoriaId)
    {
        ValidarVinculo(responsavelId, alunoId);

        if (!_db.MentoriasAlunoProfessor.Any(m => m.Id == vinculoMentoriaId && m.AlunoId == alunoId))
        {
            throw new UnauthorizedAccessException("Este chat de Mentoria nao pertence a um aluno vinculado a voce.");
        }
    }

    private AulaResumoResponse ToAulaResumo(Aula aula)
    {
        return new AulaResumoResponse(
            aula.Id,
            GetProfessorNome(aula.ProfessorId),
            GetAlunoNome(aula.AlunoId),
            aula.SolicitadaPorUsuarioId,
            GetMateriaNome(aula.MateriaId),
            aula.InicioUtc,
            aula.DuracaoMinutos,
            aula.PrecoAcordado,
            aula.Status);
    }

    private ResponsavelAcompanhamentoMentoriaResumoResponse ToAcompanhamentoResumo(AcompanhamentoMentoria acompanhamento)
    {
        var aula = _db.Aulas.Single(a => a.Id == acompanhamento.AulaId);

        return new ResponsavelAcompanhamentoMentoriaResumoResponse(
            acompanhamento.Id,
            acompanhamento.AulaId,
            acompanhamento.ProfessorId,
            GetProfessorNome(acompanhamento.ProfessorId),
            acompanhamento.VinculoMentoriaId,
            GetMateriaNome(aula.MateriaId),
            aula.InicioUtc,
            acompanhamento.Status,
            acompanhamento.CreatedAtUtc,
            acompanhamento.DataConclusaoUtc);
    }

    private string GetAlunoNome(Guid alunoId) => _db.Usuarios.OfType<Aluno>().Where(a => a.Id == alunoId).Select(a => a.Nome).SingleOrDefault() ?? "Aluno";
    private string GetProfessorNome(Guid professorId) => _db.Professores.Where(p => p.Id == professorId).Select(p => p.Nome).SingleOrDefault() ?? "Professor";
    private string GetMateriaNome(Guid materiaId) => _db.Materias.Where(m => m.Id == materiaId).Select(m => m.Nome).SingleOrDefault() ?? "Materia";
}
