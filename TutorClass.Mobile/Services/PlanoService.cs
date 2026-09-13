using TutorClass.Mobile.Models;

namespace TutorClass.Mobile.Services;

public sealed class PlanoService(SessionService session)
{
    public Task<PlanoStatusDto> ObterAsync(CancellationToken ct = default)
        => session.GetAuthenticatedAsync<PlanoStatusDto>("api/planos/me", ct);
}
