using TutorClass.Application.Abstractions;
using TutorClass.Application.DTOs;
using TutorClass.Domain.Common;
using TutorClass.Domain.Lessons;
using TutorClass.Domain.Teachers;

namespace TutorClass.Application.Services;

public sealed class AvaliacaoProfessorService
{
    private readonly IApplicationDbContext _db;

    public AvaliacaoProfessorService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<AvaliacaoProfessorResponse> AvaliarAsync(
        Guid aulaId,
        Guid alunoId,
        CriarAvaliacaoProfessorRequest request,
        CancellationToken cancellationToken)
    {
        var aula = await _db.FindAsync<Aula>(aulaId, cancellationToken)
            ?? throw new DomainException("Aula nao encontrada.");

        if (aula.AlunoId != alunoId)
        {
            throw new UnauthorizedAccessException("Somente o aluno da aula pode avaliar o professor.");
        }

        if (aula.Status != StatusAula.Concluida)
        {
            throw new DomainException("Professor so pode ser avaliado apos a aula ser concluida.");
        }

        if (_db.AvaliacoesProfessor.Any(a => a.AulaId == aulaId))
        {
            throw new DomainException("Esta aula ja possui avaliacao.");
        }

        var professor = await _db.FindAsync<Professor>(aula.ProfessorId, cancellationToken)
            ?? throw new DomainException("Professor nao encontrado.");

        var avaliacao = new AvaliacaoProfessor(aula.Id, alunoId, aula.ProfessorId, request.Nota, request.Comentario);
        _db.AddEntity(avaliacao);
        await _db.SaveChangesAsync(cancellationToken);

        var avaliacoes = _db.AvaliacoesProfessor.Where(a => a.ProfessorId == professor.Id).ToArray();
        var quantidade = avaliacoes.Length;
        decimal? media = quantidade == 0 ? null : Math.Round(avaliacoes.Average(a => (decimal)a.Nota), 2);
        professor.AtualizarAvaliacoes(media, quantidade);
        await _db.SaveChangesAsync(cancellationToken);

        var alunoNome = _db.Usuarios.Where(u => u.Id == alunoId).Select(u => u.Nome).SingleOrDefault() ?? "Aluno";
        return ToResponse(avaliacao, alunoNome);
    }

    public Task<IReadOnlyCollection<AvaliacaoProfessorResponse>> ListarDoProfessorAsync(Guid professorId, CancellationToken cancellationToken)
    {
        var avaliacoes = _db.AvaliacoesProfessor
            .Where(a => a.ProfessorId == professorId)
            .OrderByDescending(a => a.CreatedAtUtc)
            .ToArray()
            .Select(a => ToResponse(a, _db.Usuarios.Where(u => u.Id == a.AlunoId).Select(u => u.Nome).SingleOrDefault() ?? "Aluno"))
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<AvaliacaoProfessorResponse>>(avaliacoes);
    }

    private static AvaliacaoProfessorResponse ToResponse(AvaliacaoProfessor a, string alunoNome) => new(
        a.Id,
        a.AulaId,
        a.AlunoId,
        alunoNome,
        a.ProfessorId,
        a.Nota,
        a.Comentario,
        a.CreatedAtUtc);
}
