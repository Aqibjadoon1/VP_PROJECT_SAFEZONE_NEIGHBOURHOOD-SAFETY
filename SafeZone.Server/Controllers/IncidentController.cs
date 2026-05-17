using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using SafeZone.Server.DTOs;
using SafeZone.Server.Hubs;
using SafeZone.Server.Services;

namespace SafeZone.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class IncidentController : ControllerBase
{
    private readonly IIncidentService _incidentService;
    private readonly IHubContext<MapHub> _mapHub;

    public IncidentController(
        IIncidentService incidentService,
        IHubContext<MapHub> mapHub)
    {
        _incidentService = incidentService;
        _mapHub = mapHub;
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<ActionResult<List<CategoryDto>>> GetCategories()
    {
        var categories = await _incidentService.GetCategoriesAsync();
        return Ok(categories);
    }

    [HttpPost]
    public async Task<ActionResult<IncidentResponseDto>> CreateIncident(CreateIncidentDto dto)
    {
        var userId = GetCurrentUserId();
        try
        {
            var result = await _incidentService.CreateIncidentAsync(dto, userId);

            await _mapHub.Clients.All.SendAsync("NewIncidentReported", new
            {
                result.IncidentId,
                result.IncidentNumber,
                Lat = result.Latitude,
                Lng = result.Longitude,
                result.Title,
                CategoryName = result.CategoryName,
                result.Severity,
                result.Status,
                Timestamp = DateTime.UtcNow
            });

            return CreatedAtAction(nameof(GetIncident), new { id = result.IncidentId }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<IncidentResponseDto>> GetIncident(Guid id)
    {
        var incident = await _incidentService.GetIncidentByIdAsync(id);
        if (incident == null)
            return NotFound(new { message = "Incident not found" });

        return Ok(incident);
    }

    [HttpGet("my")]
    public async Task<ActionResult<List<IncidentListDto>>> GetMyIncidents()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var incidents = await _incidentService.GetMyIncidentsAsync(userId.Value);
        return Ok(incidents);
    }

    [HttpGet]
    [Authorize(Roles = "Authority,SuperAdmin")]
    public async Task<ActionResult<List<IncidentListDto>>> GetAllIncidents(
        [FromQuery] int? status,
        [FromQuery] int? severity,
        [FromQuery] Guid? categoryId)
    {
        var incidents = await _incidentService.GetAllIncidentsAsync(
            status.HasValue ? (Models.IncidentStatus)status.Value : null,
            severity.HasValue ? (Models.SeverityLevel)severity.Value : null,
            categoryId);

        return Ok(incidents);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<IncidentResponseDto>> UpdateIncident(Guid id, UpdateIncidentDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _incidentService.UpdateIncidentAsync(id, dto, userId);

        if (result == null)
            return NotFound(new { message = "Incident not found" });

        if (dto.Status.HasValue)
        {
            await _mapHub.Clients.All.SendAsync("IncidentUpdated", new
            {
                result.IncidentId,
                Status = dto.Status.Value.ToString(),
                Timestamp = DateTime.UtcNow
            });
        }

        return Ok(result);
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Authority,SuperAdmin")]
    public async Task<ActionResult<IncidentResponseDto>> UpdateStatus(Guid id, [FromQuery] int status)
    {
        var userId = GetCurrentUserId();
        var incidentStatus = (Models.IncidentStatus)status;

        var result = await _incidentService.UpdateStatusAsync(id, incidentStatus, userId);
        if (result == null)
            return NotFound(new { message = "Incident not found" });

        await _mapHub.Clients.All.SendAsync("IncidentUpdated", new
        {
            result.IncidentId,
            Status = incidentStatus.ToString(),
            Timestamp = DateTime.UtcNow
        });

        if (incidentStatus == Models.IncidentStatus.Resolved || 
            incidentStatus == Models.IncidentStatus.Closed)
        {
            await _mapHub.Clients.All.SendAsync("IncidentResolved", new
            {
                result.IncidentId,
                Timestamp = DateTime.UtcNow
            });
        }

        return Ok(result);
    }

    [HttpGet("stats/counts")]
    [Authorize(Roles = "Authority,SuperAdmin")]
    public async Task<ActionResult<object>> GetStats()
    {
        var pending = await _incidentService.GetIncidentCountByStatusAsync(Models.IncidentStatus.Pending);
        var assigned = await _incidentService.GetIncidentCountByStatusAsync(Models.IncidentStatus.Assigned);
        var inProgress = await _incidentService.GetIncidentCountByStatusAsync(Models.IncidentStatus.InProgress);
        var resolved = await _incidentService.GetIncidentCountByStatusAsync(Models.IncidentStatus.Resolved);
        var bySeverity = await _incidentService.GetIncidentCountBySeverityAsync();

        return Ok(new
        {
            statusCounts = new
            {
                pending,
                assigned,
                inProgress,
                resolved
            },
            severityCounts = bySeverity
        });
    }
}
