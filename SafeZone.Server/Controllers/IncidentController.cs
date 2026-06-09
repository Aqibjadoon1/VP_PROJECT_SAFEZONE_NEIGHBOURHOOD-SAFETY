using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using SafeZone.Server.DTOs;
using SafeZone.Server.Hubs;
using SafeZone.Server.Models;
using SafeZone.Server.Services;

namespace SafeZone.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class IncidentController : ControllerBase
{
    private readonly IIncidentService _incidentService;
    private readonly IHubContext<MapHub> _mapHub;
    private readonly ILogger<IncidentController> _logger;

    public IncidentController(
        IIncidentService incidentService,
        IHubContext<MapHub> mapHub,
        ILogger<IncidentController> logger)
    {
        _incidentService = incidentService;
        _mapHub = mapHub;
        _logger = logger;
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private bool IsAuthorityOrHigher() => User.IsInRole("Authority") || User.IsInRole("Admin") || User.IsInRole("SuperAdmin");

    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<ActionResult<List<CategoryDto>>> GetCategories()
    {
        var categories = await _incidentService.GetCategoriesAsync();
        return Ok(categories);
    }

    [HttpPost]
    public async Task<ActionResult<IncidentResponseDto>> CreateIncident([FromBody] CreateIncidentDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "Your session is no longer valid. Please sign in again." });

        try
        {
            var result = await _incidentService.CreateIncidentAsync(dto, userId);

            try
            {
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
            }
            catch (Exception hubEx)
            {
                _logger.LogError(hubEx, "Failed to broadcast NewIncidentReported to SignalR hub");
            }

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

        var userId = GetCurrentUserId();
        if (!IsAuthorityOrHigher() && incident.ReporterId != userId)
            return Forbid();

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
    [Authorize(Roles = "Authority,Admin,SuperAdmin")]
    public async Task<ActionResult<List<IncidentListDto>>> GetAllIncidents(
        [FromQuery] int? status,
        [FromQuery] int? severity,
        [FromQuery] Guid? categoryId)
    {
        IncidentStatus? incidentStatus = null;
        if (status.HasValue)
        {
            if (!Enum.IsDefined(typeof(IncidentStatus), status.Value))
                return BadRequest(new { message = "Invalid status value." });
            incidentStatus = (IncidentStatus)status.Value;
        }

        SeverityLevel? severityLevel = null;
        if (severity.HasValue)
        {
            if (!Enum.IsDefined(typeof(SeverityLevel), severity.Value))
                return BadRequest(new { message = "Invalid severity value." });
            severityLevel = (SeverityLevel)severity.Value;
        }

        var incidents = await _incidentService.GetAllIncidentsAsync(incidentStatus, severityLevel, categoryId);
        return Ok(incidents);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<IncidentResponseDto>> UpdateIncident(Guid id, [FromBody] UpdateIncidentDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();

        var incident = await _incidentService.GetIncidentByIdAsync(id);
        if (incident == null)
            return NotFound(new { message = "Incident not found" });

        if (!IsAuthorityOrHigher() && incident.ReporterId != userId)
            return Forbid();

        var result = await _incidentService.UpdateIncidentAsync(id, dto, userId);
        if (result == null)
            return NotFound(new { message = "Incident not found" });

        if (dto.Status.HasValue)
        {
            try
            {
                await _mapHub.Clients.All.SendAsync("IncidentUpdated", new
                {
                    result.IncidentId,
                    Status = dto.Status.Value.ToString(),
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception hubEx)
            {
                _logger.LogError(hubEx, "Failed to broadcast IncidentUpdated to SignalR hub");
            }
        }

        return Ok(result);
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Authority,Admin,SuperAdmin")]
    public async Task<ActionResult<IncidentResponseDto>> UpdateStatus(Guid id, [FromQuery] int status)
    {
        if (!Enum.IsDefined(typeof(IncidentStatus), status))
            return BadRequest(new { message = "Invalid status value." });

        var userId = GetCurrentUserId();
        var incidentStatus = (IncidentStatus)status;

        var result = await _incidentService.UpdateStatusAsync(id, incidentStatus, userId);
        if (result == null)
            return NotFound(new { message = "Incident not found" });

        try
        {
            await _mapHub.Clients.All.SendAsync("IncidentUpdated", new
            {
                result.IncidentId,
                Status = incidentStatus.ToString(),
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception hubEx)
        {
            _logger.LogError(hubEx, "Failed to broadcast IncidentUpdated to SignalR hub");
        }

        if (incidentStatus == IncidentStatus.Resolved ||
            incidentStatus == IncidentStatus.Closed)
        {
            try
            {
                await _mapHub.Clients.All.SendAsync("IncidentResolved", new
                {
                    result.IncidentId,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception hubEx)
            {
                _logger.LogError(hubEx, "Failed to broadcast IncidentResolved to SignalR hub");
            }
        }

        return Ok(result);
    }

    [HttpGet("stats/counts")]
    [Authorize(Roles = "Authority,Admin,SuperAdmin")]
    public async Task<ActionResult<object>> GetStats()
    {
        var pending = await _incidentService.GetIncidentCountByStatusAsync(IncidentStatus.Pending);
        var assigned = await _incidentService.GetIncidentCountByStatusAsync(IncidentStatus.Assigned);
        var inProgress = await _incidentService.GetIncidentCountByStatusAsync(IncidentStatus.InProgress);
        var resolved = await _incidentService.GetIncidentCountByStatusAsync(IncidentStatus.Resolved);
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
