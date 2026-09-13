using TutorClass.Mobile.Models;

namespace TutorClass.Mobile.Services;

public sealed class TutorIaService(SessionService session)
{
    public Task<TutorIaResponse> StatusAsync(CancellationToken ct = default)
        => session.GetAuthenticatedAsync<TutorIaResponse>("api/tutoria/status", ct);
    public Task<TutorIaResponse> EnviarAsync(string mensagem, CancellationToken ct = default)
        => session.PostAuthenticatedAsync<TutorIaResponse>("api/tutoria/chat", new TutorIaChatRequest(mensagem), ct);
}

public sealed class NotificacaoService(SessionService session)
{
    public const int PageSize = 20;
    public Task<NotificacaoDto[]> ListarAsync(int page, CancellationToken ct = default)
        => session.GetAuthenticatedAsync<NotificacaoDto[]>($"api/notificacoes?page={page}&pageSize={PageSize}", ct);
    public Task<NotificacaoResumoDto> ResumoAsync(CancellationToken ct = default)
        => session.GetAuthenticatedAsync<NotificacaoResumoDto>("api/notificacoes/resumo", ct);
    public Task<NotificacaoDto> MarcarLidaAsync(Guid id, CancellationToken ct = default)
        => session.PostAuthenticatedAsync<NotificacaoDto>($"api/notificacoes/{id:D}/marcar-lida", ct: ct);
    public Task<MarcarTodasLidasDto> MarcarTodasAsync(CancellationToken ct = default)
        => session.PostAuthenticatedAsync<MarcarTodasLidasDto>("api/notificacoes/marcar-todas-lidas", ct: ct);
}
