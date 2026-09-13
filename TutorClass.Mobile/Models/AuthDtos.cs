namespace TutorClass.Mobile.Models;

public sealed record LoginRequest(string Email, string Senha);
public sealed record UsuarioAutenticadoDto(Guid Id, string Nome, string Email, string Perfil);
public sealed record LoginResponse(string AccessToken, DateTime ExpiresAtUtc, UsuarioAutenticadoDto Usuario);
