using TutorClass.Application.Abstractions;
using TutorClass.Application.DTOs;
using TutorClass.Domain.Common;
using TutorClass.Domain.Lessons;
using TutorClass.Domain.Teachers;

namespace TutorClass.Application.Services;

public sealed class DisponibilidadeProfessorService
{
    private readonly IApplicationDbContext _db;

    public DisponibilidadeProfessorService(IApplicationDbContext db)
    {
        _db = db;
    }

    public Task<IReadOnlyCollection<DisponibilidadeProfessorDto>> ListarPublicasAsync(Guid professorId, CancellationToken cancellationToken)
    {
        EnsureProfessorPublico(professorId);
        return Task.FromResult<IReadOnlyCollection<DisponibilidadeProfessorDto>>(ListarPorProfessor(professorId));
    }

    public Task<IReadOnlyCollection<DisponibilidadeProfessorDto>> ListarMinhasAsync(Guid professorId, CancellationToken cancellationToken)
    {
        EnsureProfessorAtivo(professorId);
        return Task.FromResult<IReadOnlyCollection<DisponibilidadeProfessorDto>>(ListarPorProfessor(professorId));
    }

    public Task<IReadOnlyCollection<HorarioLivreProfessorDto>> ListarHorariosLivresAsync(Guid professorId, DateOnly data, CancellationToken cancellationToken)
    {
        EnsureProfessorPublico(professorId);

        var disponibilidade = _db.DisponibilidadesProfessor
            .Where(d => d.ProfessorId == professorId && d.DiaSemana == data.DayOfWeek)
            .OrderBy(d => d.Inicio)
            .ToArray();

        var inicioDia = data.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var fimDia = data.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
        var aulasConfirmadas = _db.Aulas
            .Where(a => a.ProfessorId == professorId && a.Status == StatusAula.Confirmada)
            .AsEnumerable()
            .Where(a => a.InicioUtc < fimDia && inicioDia < a.FimUtc)
            .ToArray();

        var livres = new List<HorarioLivreProfessorDto>();
        foreach (var item in disponibilidade)
        {
            var segmentos = new List<(TimeOnly Inicio, TimeOnly Fim)> { (item.Inicio, item.Fim) };
            foreach (var aula in aulasConfirmadas)
            {
                segmentos = SubtrairAula(segmentos, TimeOnly.FromDateTime(aula.InicioUtc), TimeOnly.FromDateTime(aula.FimUtc));
            }

            livres.AddRange(segmentos
                .Where(segmento => segmento.Inicio < segmento.Fim)
                .Select(segmento => new HorarioLivreProfessorDto(data, segmento.Inicio, segmento.Fim)));
        }

        return Task.FromResult<IReadOnlyCollection<HorarioLivreProfessorDto>>(livres);
    }

    public async Task<DisponibilidadeProfessorDto> CadastrarAsync(Guid professorId, CadastrarDisponibilidadeProfessorRequest request, CancellationToken cancellationToken)
    {
        EnsureProfessorAtivo(professorId);
        EnsureNoOverlap(professorId, null, request.DiaSemana, request.Inicio, request.Fim);

        var disponibilidade = new DisponibilidadeProfessor(professorId, request.DiaSemana, request.Inicio, request.Fim);
        _db.AddEntity(disponibilidade);
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(disponibilidade);
    }

    public async Task<DisponibilidadeProfessorDto> AtualizarAsync(Guid professorId, Guid disponibilidadeId, AtualizarDisponibilidadeProfessorRequest request, CancellationToken cancellationToken)
    {
        EnsureProfessorAtivo(professorId);
        var disponibilidade = await _db.FindAsync<DisponibilidadeProfessor>(disponibilidadeId, cancellationToken)
            ?? throw new DomainException("Disponibilidade nao encontrada.");

        EnsureOwner(disponibilidade, professorId);
        EnsureNoOverlap(professorId, disponibilidadeId, request.DiaSemana, request.Inicio, request.Fim);

        disponibilidade.Atualizar(request.DiaSemana, request.Inicio, request.Fim);
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(disponibilidade);
    }

    public async Task RemoverAsync(Guid professorId, Guid disponibilidadeId, CancellationToken cancellationToken)
    {
        EnsureProfessorAtivo(professorId);
        var disponibilidade = await _db.FindAsync<DisponibilidadeProfessor>(disponibilidadeId, cancellationToken)
            ?? throw new DomainException("Disponibilidade nao encontrada.");

        EnsureOwner(disponibilidade, professorId);
        _db.RemoveEntity(disponibilidade);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private IReadOnlyCollection<DisponibilidadeProfessorDto> ListarPorProfessor(Guid professorId)
    {
        return _db.DisponibilidadesProfessor
            .Where(d => d.ProfessorId == professorId)
            .OrderBy(d => d.DiaSemana)
            .ThenBy(d => d.Inicio)
            .Select(d => ToDto(d))
            .ToArray();
    }

    private void EnsureProfessorPublico(Guid professorId)
    {
        if (!_db.Professores.Any(p => p.Id == professorId &&
            p.Ativo &&
            p.Status == StatusProfessor.Aprovado &&
            p.Candidaturas.Any(c => c.Status == StatusCandidaturaProfessor.Aprovada)))
        {
            throw new DomainException("Professor publico nao encontrado.");
        }
    }

    private void EnsureProfessorAtivo(Guid professorId)
    {
        if (!_db.Professores.Any(p => p.Id == professorId && p.Ativo))
        {
            throw new DomainException("Professor nao encontrado ou inativo.");
        }
    }

    private void EnsureNoOverlap(Guid professorId, Guid? disponibilidadeIgnoradaId, DayOfWeek diaSemana, TimeOnly inicio, TimeOnly fim)
    {
        var nova = new DisponibilidadeProfessor(professorId, diaSemana, inicio, fim);
        var existeSobreposicao = _db.DisponibilidadesProfessor
            .Where(d => d.ProfessorId == professorId && d.Id != disponibilidadeIgnoradaId)
            .AsEnumerable()
            .Any(d => d.Sobrepoe(nova.DiaSemana, nova.Inicio, nova.Fim));

        if (existeSobreposicao)
        {
            throw new DomainException("Disponibilidade sobreposta para este professor.");
        }
    }

    private static void EnsureOwner(DisponibilidadeProfessor disponibilidade, Guid professorId)
    {
        if (disponibilidade.ProfessorId != professorId)
        {
            throw new UnauthorizedAccessException("Professor nao pode alterar disponibilidade de outro professor.");
        }
    }

    private static List<(TimeOnly Inicio, TimeOnly Fim)> SubtrairAula(List<(TimeOnly Inicio, TimeOnly Fim)> segmentos, TimeOnly aulaInicio, TimeOnly aulaFim)
    {
        var livres = new List<(TimeOnly Inicio, TimeOnly Fim)>();
        foreach (var segmento in segmentos)
        {
            if (segmento.Fim <= aulaInicio || aulaFim <= segmento.Inicio)
            {
                livres.Add(segmento);
                continue;
            }

            if (segmento.Inicio < aulaInicio)
            {
                livres.Add((segmento.Inicio, aulaInicio));
            }

            if (aulaFim < segmento.Fim)
            {
                livres.Add((aulaFim, segmento.Fim));
            }
        }

        return livres;
    }

    private static DisponibilidadeProfessorDto ToDto(DisponibilidadeProfessor disponibilidade)
    {
        return new DisponibilidadeProfessorDto(disponibilidade.Id, disponibilidade.DiaSemana, disponibilidade.Inicio, disponibilidade.Fim);
    }
}
