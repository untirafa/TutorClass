using System.Net;
using System.Net.Http.Json;
using TutorClass.Mobile.Models;

namespace TutorClass.Mobile.Services;

// Uses the same HttpClient registered for the existing session service.
public sealed class ProfessorService(HttpClient http)
{
    public async Task<ProfessorResumoDto[]> ListarAsync(CancellationToken ct = default)
    {
        using var response = await http.GetAsync("api/professores", ct);
        EnsureSuccess(response);
        return await response.Content.ReadFromJsonAsync<ProfessorResumoDto[]>(ct)
            ?? throw new HttpRequestException("Resposta inválida.");
    }

    public async Task<ProfessorDetalhesDto?> BuscarAsync(Guid id, CancellationToken ct = default)
    {
        using var response = await http.GetAsync($"api/professores/{id:D}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        EnsureSuccess(response);
        return await response.Content.ReadFromJsonAsync<ProfessorDetalhesDto>(ct)
            ?? throw new HttpRequestException("Resposta inválida.");
    }

    private static void EnsureSuccess(HttpResponseMessage response) => response.EnsureSuccessStatusCode();
}
