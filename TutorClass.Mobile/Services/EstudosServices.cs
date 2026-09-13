using System.Net.Http.Json;
using TutorClass.Mobile.Models;

namespace TutorClass.Mobile.Services;

public sealed class MateriaService(HttpClient http)
{
    public async Task<MateriaDto[]> ListarAsync(CancellationToken ct = default)
    {
        using var response = await http.GetAsync("api/materias", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MateriaDto[]>(ct)
            ?? throw new HttpRequestException("Resposta inválida.");
    }
}

public sealed class AulaService(SessionService session)
{
    public Task<AulaResumoDto[]> ListarMinhasAsync(CancellationToken ct = default)
        => session.GetAuthenticatedAsync<AulaResumoDto[]>("api/aulas/minhas", ct);
}
