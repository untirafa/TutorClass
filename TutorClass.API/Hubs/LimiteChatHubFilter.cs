using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using TutorClass.API.Security;

namespace TutorClass.API.Hubs;

public sealed class LimiteChatHubFilter(ControleAbuso controle) : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext context, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var id = context.Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (id is not null)
        {
            var envio = context.HubMethodName == nameof(MentoriaChatHub.EnviarMensagem);
            var retry = controle.Tentar(envio ? "mentoria" : "hub-operacoes", "user:" + id, envio ? 60 : 120);
            if (retry > 0) throw new HubException($"LIMITE_REQUISICOES: Aguarde {retry} segundos e tente novamente.");
        }
        return await next(context);
    }
}
