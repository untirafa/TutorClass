using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorClass.API.Security;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;

namespace TutorClass.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthService _service;

    public AuthController(AuthService service)
    {
        _service = service;
    }

    [AllowAnonymous]
    [HttpPost("registrar/aluno")]
    [LimiteRequisicoes("cadastro", 10)]
    [ProducesResponseType(typeof(UsuarioAutenticadoDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<UsuarioAutenticadoDto>> RegistrarAluno(RegistrarAlunoRequest request, CancellationToken cancellationToken)
    {
        var usuario = await _service.RegistrarAlunoAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Me), usuario);
    }

    [AllowAnonymous]
    [HttpPost("registrar/responsavel")]
    [LimiteRequisicoes("cadastro", 10)]
    [ProducesResponseType(typeof(UsuarioAutenticadoDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<UsuarioAutenticadoDto>> RegistrarResponsavel(RegistrarResponsavelRequest request, CancellationToken cancellationToken)
    {
        var usuario = await _service.RegistrarResponsavelAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Me), usuario);
    }

    [AllowAnonymous]
    [HttpPost("registrar/professor")]
    [LimiteRequisicoes("cadastro", 10)]
    [ProducesResponseType(typeof(UsuarioAutenticadoDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<UsuarioAutenticadoDto>> RegistrarProfessor(RegistrarProfessorRequest request, CancellationToken cancellationToken)
    {
        var usuario = await _service.RegistrarProfessorAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Me), usuario);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [LimiteRequisicoes("login", 20)]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _service.LoginAsync(request, cancellationToken));
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UsuarioAutenticadoDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UsuarioAutenticadoDto>> Me(CancellationToken cancellationToken)
    {
        return Ok(await _service.BuscarUsuarioAtualAsync(User.GetUserId(), cancellationToken));
    }
}
