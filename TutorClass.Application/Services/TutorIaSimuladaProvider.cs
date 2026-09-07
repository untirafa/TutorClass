using TutorClass.Application.Abstractions;
using TutorClass.Application.DTOs;

namespace TutorClass.Application.Services;

public sealed class TutorIaSimuladaProvider : ITutorIaProvider
{
    public Task<string> ResponderAsync(TutorIaProviderRequest request, CancellationToken cancellationToken)
    {
        var pergunta = request.Mensagem.Trim();
        var resposta = $"TutorIA simulada: vamos estudar isso passo a passo. Voce perguntou: \"{pergunta}\". Comece identificando o conceito principal, depois veja um exemplo simples e tente explicar com suas proprias palavras.";
        return Task.FromResult(resposta);
    }
}
