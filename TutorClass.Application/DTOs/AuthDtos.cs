namespace TutorClass.Application.DTOs;

public sealed record RegistrarAlunoRequest(string Nome, string Email, string Senha, DateOnly? DataNascimento);

public sealed record RegistrarResponsavelRequest(string Nome, string Email, string Senha, string? Telefone);

public sealed record RegistrarProfessorRequest(string Nome, string Email, string Senha, string Biografia, decimal PrecoHora);

public sealed record LoginRequest(string Email, string Senha);

public sealed record UsuarioAutenticadoDto(Guid Id, string Nome, string Email, string Perfil);

public sealed record LoginResponse(string AccessToken, DateTime ExpiresAtUtc, UsuarioAutenticadoDto Usuario);
