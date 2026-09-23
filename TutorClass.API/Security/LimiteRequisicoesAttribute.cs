using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TutorClass.API.Security;

[AttributeUsage(AttributeTargets.Method)]
public sealed class LimiteRequisicoesAttribute(string grupo, int limite) : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var http = context.HttpContext;
        var authenticated = http.User.Identity?.IsAuthenticated == true;
        // Authentication endpoints always use the network peer, never an arbitrary login name.
        var identity = grupo is "login" or "cadastro" || !authenticated
            ? "ip:" + (http.Connection.RemoteIpAddress?.ToString() ?? "local")
            : "user:" + http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var retry = http.RequestServices.GetRequiredService<ControleAbuso>().Tentar(grupo, identity, limite);
        if (retry == 0) return;
        http.Response.Headers.RetryAfter = retry.ToString(System.Globalization.CultureInfo.InvariantCulture);
        context.Result = new ObjectResult(new { codigo = "LIMITE_REQUISICOES", error = "Muitas tentativas em pouco tempo. Aguarde e tente novamente.", retryAfterSeconds = retry })
            { StatusCode = StatusCodes.Status429TooManyRequests };
    }
}
