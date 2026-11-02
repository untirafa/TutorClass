using System.Diagnostics;

namespace TutorClass.API.Middleware;

public sealed class TutorIaPerformanceMiddleware(RequestDelegate next, ILogger<TutorIaPerformanceMiddleware> logger)
{
    private static readonly object TimingKey = new();

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.TrimEnd('/');
        if (!HttpMethods.IsPost(context.Request.Method) ||
            !(string.Equals(path, "/api/tutoria/chat", StringComparison.OrdinalIgnoreCase) ||
              string.Equals(path, "/api/tutoria/chat/stream", StringComparison.OrdinalIgnoreCase)))
        {
            await next(context);
            return;
        }

        var start = Stopwatch.GetTimestamp();
        context.Items[TimingKey] = (Action)(() => logger.LogInformation(
            "TutorIA Performance Stage={Stage} DurationMs={DurationMs}",
            "EntradaAteProcessamento", Stopwatch.GetElapsedTime(start).TotalMilliseconds));
        try { await next(context); }
        finally
        {
            logger.LogInformation("TutorIA Performance Stage={Stage} DurationMs={DurationMs}",
                "RequisicaoTotal", Stopwatch.GetElapsedTime(start).TotalMilliseconds);
            context.Items.Remove(TimingKey);
        }
    }

    public static void MarcarInicio(HttpContext context)
    {
        if (context.Items.TryGetValue(TimingKey, out var value) && value is Action mark) mark();
    }
}
