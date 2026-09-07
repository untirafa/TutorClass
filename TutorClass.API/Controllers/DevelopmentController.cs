using Microsoft.AspNetCore.Mvc;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;

namespace TutorClass.API.Controllers;

[ApiController]
[Route("api/dev")]
public sealed class DevelopmentController : ControllerBase
{
    private readonly DevelopmentDataService _service;
    private readonly IWebHostEnvironment _environment;

    public DevelopmentController(DevelopmentDataService service, IWebHostEnvironment environment)
    {
        _service = service;
        _environment = environment;
    }

    [HttpPost("test-data")]
    [ProducesResponseType(typeof(DevelopmentTestDataDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DevelopmentTestDataDto>> EnsureTestData(CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        return Ok(await _service.EnsureTestDataAsync(cancellationToken));
    }
}
