using TutorClass.Application.DTOs;

namespace TutorClass.Application.Abstractions;

public interface ITutorIaProvider
{
    Task<string> ResponderAsync(TutorIaProviderRequest request, CancellationToken cancellationToken);
}
