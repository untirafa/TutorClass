using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;

namespace TutorClass.API.Controllers;

[ApiController]
[Route("api/admin/analytics")]
[Authorize(Policy = "Administrador")]
public sealed class AdminAnalyticsController : ControllerBase
{
    private readonly AnalyticsService _service;

    public AdminAnalyticsController(AnalyticsService service)
    {
        _service = service;
    }

    [HttpGet("resumo")]
    [ProducesResponseType(typeof(AnalyticsResumoResponse), StatusCodes.Status200OK)]
    public async Task<AnalyticsResumoResponse> Resumo(CancellationToken cancellationToken)
    {
        return await _service.ObterResumoAsync(cancellationToken);
    }

    [HttpGet("materias")]
    [ProducesResponseType(typeof(IReadOnlyCollection<AnalyticsMateriaRankingResponse>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<AnalyticsMateriaRankingResponse>> Materias(CancellationToken cancellationToken)
    {
        return await _service.ListarRankingMateriasAsync(cancellationToken);
    }

    [HttpGet("demanda-materias")]
    [ProducesResponseType(typeof(IReadOnlyCollection<DemandaMateriaResponse>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<DemandaMateriaResponse>> DemandaMaterias(CancellationToken cancellationToken)
    {
        return await _service.PreverDemandaMateriasAsync(cancellationToken);
    }
}
