using Microsoft.AspNetCore.Mvc;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;

namespace TutorClass.API.Controllers;

[ApiController]
[Route("api/materias")]
public sealed class MateriasController : ControllerBase
{
    private readonly MateriaService _service;

    public MateriasController(MateriaService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<MateriaDto>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<MateriaDto>> Listar(CancellationToken cancellationToken)
    {
        return await _service.ListarAsync(cancellationToken);
    }
}
