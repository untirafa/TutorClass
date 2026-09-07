using Microsoft.AspNetCore.Identity;
using TutorClass.Application.Abstractions;
using TutorClass.Application.DTOs;
using TutorClass.Domain.Common;
using TutorClass.Domain.Teachers;
using TutorClass.Domain.Users;

namespace TutorClass.Application.Services;

public sealed class AuthService
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher<Usuario> _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(
        IApplicationDbContext db,
        IPasswordHasher<Usuario> passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<UsuarioAutenticadoDto> RegistrarAlunoAsync(RegistrarAlunoRequest request, CancellationToken cancellationToken)
    {
        EnsurePasswordIsValid(request.Senha);
        var email = NormalizeEmail(request.Email);
        EnsureEmailIsUnique(email);

        var aluno = new Aluno(request.Nome, email, "pending", request.DataNascimento);
        aluno.DefinirPasswordHash(_passwordHasher.HashPassword(aluno, request.Senha));

        _db.AddEntity(aluno);
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(aluno);
    }

    public async Task<UsuarioAutenticadoDto> RegistrarResponsavelAsync(RegistrarResponsavelRequest request, CancellationToken cancellationToken)
    {
        EnsurePasswordIsValid(request.Senha);
        var email = NormalizeEmail(request.Email);
        EnsureEmailIsUnique(email);

        var responsavel = new Responsavel(request.Nome, email, "pending", request.Telefone);
        responsavel.DefinirPasswordHash(_passwordHasher.HashPassword(responsavel, request.Senha));

        _db.AddEntity(responsavel);
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(responsavel);
    }

    public async Task<UsuarioAutenticadoDto> RegistrarProfessorAsync(RegistrarProfessorRequest request, CancellationToken cancellationToken)
    {
        EnsurePasswordIsValid(request.Senha);
        var email = NormalizeEmail(request.Email);
        EnsureEmailIsUnique(email);

        var professor = new Professor(request.Nome, email, "pending");
        professor.DefinirPasswordHash(_passwordHasher.HashPassword(professor, request.Senha));
        professor.AtualizarPerfilProfissional(request.Biografia, request.PrecoHora);

        _db.AddEntity(professor);
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(professor);
    }

    public Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        var usuario = _db.Usuarios.SingleOrDefault(u => u.Email == email && u.Ativo)
            ?? throw new DomainException("Email ou senha invalidos.");

        var result = _passwordHasher.VerifyHashedPassword(usuario, usuario.PasswordHash, request.Senha);
        if (result == PasswordVerificationResult.Failed)
        {
            throw new DomainException("Email ou senha invalidos.");
        }

        return Task.FromResult(_jwtTokenService.CreateToken(usuario));
    }

    public Task<UsuarioAutenticadoDto> BuscarUsuarioAtualAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var usuario = _db.Usuarios.SingleOrDefault(u => u.Id == usuarioId && u.Ativo)
            ?? throw new DomainException("Usuario autenticado nao encontrado.");

        return Task.FromResult(ToDto(usuario));
    }

    private void EnsureEmailIsUnique(string email)
    {
        if (_db.Usuarios.Any(u => u.Email == email))
        {
            throw new DomainException("Email ja cadastrado.");
        }
    }

    private static void EnsurePasswordIsValid(string senha)
    {
        if (string.IsNullOrWhiteSpace(senha) || senha.Length < 8)
        {
            throw new DomainException("Senha deve possuir pelo menos 8 caracteres.");
        }
    }

    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("Email e obrigatorio.");
        }

        return email.Trim().ToLowerInvariant();
    }

    private static UsuarioAutenticadoDto ToDto(Usuario usuario)
    {
        return new UsuarioAutenticadoDto(usuario.Id, usuario.Nome, usuario.Email, usuario.GetType().Name);
    }
}
