using TutorClass.Application.DTOs;

namespace TutorClass.Application.Abstractions;

public interface IContextoIaService
{
    Task<ContextoIaAluno> ObterContextoAsync(Guid alunoId, CancellationToken cancellationToken = default);
}
