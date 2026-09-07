using TutorClass.Application.DTOs;
using TutorClass.Domain.Users;

namespace TutorClass.Application.Abstractions;

public interface IJwtTokenService
{
    LoginResponse CreateToken(Usuario usuario);
}
