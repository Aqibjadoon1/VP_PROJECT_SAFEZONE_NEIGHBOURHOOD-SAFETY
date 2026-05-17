using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeZone.Server.DTOs;
using SafeZone.Server.Services;

namespace SafeZone.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MapController : ControllerBase
{
    private readonly IIncidentService _incidentService;

    public MapController(IIncidentService incidentService)
    {
        _incidentService = incidentService;
    }

    [HttpGet("incidents")]
    [AllowAnonymous]
    public async Task<ActionResult<List<MapIncidentDto>>> GetMapIncidents(
        [FromQuery] double? minLat,
        [FromQuery] double? maxLat,
        [FromQuery] double? minLng,
        [FromQuery] double? maxLng,
        [FromQuery] int? status)
    {
        var incidents = await _incidentService.GetIncidentsForMapAsync(
            minLat, maxLat, minLng, maxLng,
            status.HasValue ? (Models.IncidentStatus)status.Value : null);

        return Ok(incidents);
    }

    [HttpGet("heatmap")]
    [AllowAnonymous]
    public async Task<ActionResult<List<HeatmapPointDto>>> GetHeatmapData(
        [FromQuery] int? daysBack)
    {
        DateTime? since = null;
        if (daysBack.HasValue)
        {
            since = DateTime.UtcNow.AddDays(-daysBack.Value);
        }

        var data = await _incidentService.GetHeatmapDataAsync(since);
        return Ok(data);
    }

    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<ActionResult<List<CategoryDto>>> GetCategories()
    {
        var categories = await _incidentService.GetCategoriesAsync();
        return Ok(categories);
    }
}
