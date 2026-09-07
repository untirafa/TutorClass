using TutorClass.Application.Abstractions;
using TutorClass.Application.DTOs;
using TutorClass.Domain.Common;
using TutorClass.Domain.Lessons;

namespace TutorClass.Application.Services;

public sealed class AcompanhamentoAulaService
{
    private readonly IApplicationDbContext _db;

    public AcompanhamentoAulaService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<AcompanhamentoAulaResponse> RegistrarOuAtualizarAsync(
        Guid aulaId,
        Guid professorId,
        RegistrarAcompanhamentoAulaRequest request,
        CancellationToken cancellationToken)
    {
        var aula = await _db.FindAsync<Aula>(aulaId, cancellationToken)
            ?? throw new DomainException("Aula nao encontrada.");

        if (aula.ProfessorId != professorId)
        {
            throw new UnauthorizedAccessException("Somente o professor da aula pode registrar o acompanhamento.");
        }

        if (aula.Status != StatusAula.Concluida)
        {
            throw new DomainException("Acompanhamento so pode ser registrado apos a aula ser concluida.");
        }

        var acompanhamento = _db.AcompanhamentosAula.SingleOrDefault(a => a.AulaId == aulaId);
        if (acompanhamento is null)
        {
            acompanhamento = new AcompanhamentoAula(
                aula.Id,
                aula.AlunoId,
                aula.ProfessorId,
                request.Resumo,
                request.PontosFortes,
                request.PontosMelhorar,
                request.ProximosPassos);
            _db.AddEntity(acompanhamento);
        }
        else
        {
            acompanhamento.Atualizar(request.Resumo, request.PontosFortes, request.PontosMelhorar, request.ProximosPassos);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(acompanhamento);
    }

    public Task<AcompanhamentoAulaResponse?> BuscarAsync(Guid aulaId, Guid usuarioId, bool administrador, CancellationToken cancellationToken)
    {
        var aula = _db.Aulas.SingleOrDefault(a => a.Id == aulaId);
        if (aula is null)
        {
            return Task.FromResult<AcompanhamentoAulaResponse?>(null);
        }

        if (!administrador && aula.AlunoId != usuarioId && aula.ProfessorId != usuarioId)
        {
            throw new UnauthorizedAccessException("Usuario nao pode visualizar acompanhamento de outra aula.");
        }

        var acompanhamento = _db.AcompanhamentosAula.SingleOrDefault(a => a.AulaId == aulaId);
        return Task.FromResult(acompanhamento is null ? null : ToResponse(acompanhamento));
    }

    private static AcompanhamentoAulaResponse ToResponse(AcompanhamentoAula a) => new(
        a.Id,
        a.AulaId,
        a.AlunoId,
        a.ProfessorId,
        a.Resumo,
        a.PontosFortes,
        a.PontosMelhorar,
        a.ProximosPassos,
        a.CreatedAtUtc,
        a.UpdatedAtUtc);
}
