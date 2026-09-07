using TutorClass.Application.Abstractions;
using TutorClass.Application.DTOs;

namespace TutorClass.Application.Services;

public sealed class MateriaService
{
    private readonly IApplicationDbContext _db;

    public MateriaService(IApplicationDbContext db)
    {
        _db = db;
    }

    public Task<IReadOnlyCollection<MateriaDto>> ListarAsync(CancellationToken cancellationToken)
    {
        var materias = _db.Materias
            .OrderBy(m => m.Nome)
            .Select(m => new MateriaDto(m.Id, m.Nome))
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<MateriaDto>>(materias);
    }
}
