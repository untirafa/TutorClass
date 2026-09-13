using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TutorClass.Mobile.Models;

namespace TutorClass.Mobile.Services;

public interface ITokenStore
{
    Task<string?> ReadAsync();
    Task SaveAsync(string token);
    void Clear();
}

public sealed class SessionException(string message) : Exception(message);

public sealed class SessionService(HttpClient http, ITokenStore tokens)
{
    public UsuarioAutenticadoDto? Usuario { get; private set; }

    public async Task LoginAsync(string email, string senha, CancellationToken cancellationToken = default)
    {
        using var response = await http.PostAsJsonAsync("api/auth/login",
            new LoginRequest(email.Trim(), senha), cancellationToken);
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest or HttpStatusCode.Forbidden)
            throw new SessionException("Email ou senha inválidos.");
        EnsureAvailable(response);
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken);
        if (login is null || string.IsNullOrWhiteSpace(login.AccessToken))
            throw new SessionException("Não foi possível iniciar a sessão. Tente novamente.");
        // Confirm identity with the API; do not infer authorization from JWT claims.
        var usuario = await GetMeAsync(login.AccessToken, cancellationToken);
        await tokens.SaveAsync(login.AccessToken);
        Usuario = usuario;
    }

    public async Task<bool> RestoreAsync(CancellationToken cancellationToken = default)
    {
        var token = await tokens.ReadAsync();
        if (string.IsNullOrWhiteSpace(token)) return false;
        try
        {
            Usuario = await GetMeAsync(token, cancellationToken);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            Logout();
            return false;
        }
    }

    private async Task<UsuarioAutenticadoDto> GetMeAsync(string token, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await http.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new UnauthorizedAccessException("Sua sessão expirou. Entre novamente.");
        EnsureAvailable(response);
        return await response.Content.ReadFromJsonAsync<UsuarioAutenticadoDto>(cancellationToken)
            ?? throw new SessionException("Não foi possível confirmar sua sessão.");
    }

    public Task<T> GetAuthenticatedAsync<T>(string path, CancellationToken ct = default)
        => SendAuthenticatedAsync<T>(HttpMethod.Get, path, null, ct);

    public Task<T> PostAuthenticatedAsync<T>(string path, object? body = null, CancellationToken ct = default)
        => SendAuthenticatedAsync<T>(HttpMethod.Post, path, body, ct);

    private async Task<T> SendAuthenticatedAsync<T>(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        var token = await tokens.ReadAsync();
        if (string.IsNullOrWhiteSpace(token))
        {
            Logout();
            throw new UnauthorizedAccessException();
        }
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) request.Content = JsonContent.Create(body);
        using var response = await http.SendAsync(request, ct);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            Logout();
            throw new UnauthorizedAccessException();
        }
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(ct)
            ?? throw new HttpRequestException("Resposta inválida.");
    }

    public void Logout()
    {
        tokens.Clear();
        Usuario = null;
    }

    private static void EnsureAvailable(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
            throw new SessionException("O TutorClass está indisponível no momento. Tente novamente.");
    }
}


