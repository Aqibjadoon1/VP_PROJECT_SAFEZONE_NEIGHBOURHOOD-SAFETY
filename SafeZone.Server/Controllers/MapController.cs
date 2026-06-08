using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeZone.Server.DTOs;
using SafeZone.Server.Models;
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
        if (minLat is < -90 or > 90 || maxLat is < -90 or > 90)
            return BadRequest(new { message = "Latitude must be between -90 and 90." });
        if (minLng is < -180 or > 180 || maxLng is < -180 or > 180)
            return BadRequest(new { message = "Longitude must be between -180 and 180." });
        if (minLat.HasValue && maxLat.HasValue && minLat > maxLat)
            return BadRequest(new { message = "minLat must be less than or equal to maxLat." });
        if (minLng.HasValue && maxLng.HasValue && minLng > maxLng)
            return BadRequest(new { message = "minLng must be less than or equal to maxLng." });

        IncidentStatus? incidentStatus = null;
        if (status.HasValue)
        {
            if (!Enum.IsDefined(typeof(IncidentStatus), status.Value))
                return BadRequest(new { message = "Invalid status value." });
            incidentStatus = (IncidentStatus)status.Value;
        }

        var incidents = await _incidentService.GetIncidentsForMapAsync(
            minLat, maxLat, minLng, maxLng, incidentStatus);

        return Ok(incidents);
    }

    [HttpGet("heatmap")]
    [AllowAnonymous]
    public async Task<ActionResult<List<HeatmapPointDto>>> GetHeatmapData(
        [FromQuery] int? daysBack)
    {
        if (daysBack.HasValue && (daysBack < 1 || daysBack > 730))
            return BadRequest(new { message = "daysBack must be between 1 and 730." });

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
