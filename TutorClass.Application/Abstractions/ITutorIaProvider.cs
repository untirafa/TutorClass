using TutorClass.Application.DTOs;

namespace TutorClass.Application.Abstractions;

public interface ITutorIaProvider
{
    Task<string> ResponderAsync(TutorIaProviderRequest request, CancellationToken cancellationToken);

    async Task<string> ResponderProgressivamenteAsync(TutorIaProviderRequest request,
        Func<string, CancellationToken, Task> receberTexto, CancellationToken cancellationToken)
    {
        var resposta = await ResponderAsync(request, cancellationToken);
        await receberTexto(resposta, cancellationToken);
        return resposta;
    }
}
