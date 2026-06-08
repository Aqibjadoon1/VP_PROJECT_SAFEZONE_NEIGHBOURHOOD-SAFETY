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
[Authorize(Roles = "Authority,SuperAdmin")]
public class AlertController : ControllerBase
{
    private readonly IAlertService _alertService;
    private readonly IHubContext<AlertHub> _alertHub;
    private readonly ILogger<AlertController> _logger;

    public AlertController(
        IAlertService alertService,
        IHubContext<AlertHub> alertHub,
        ILogger<AlertController> logger)
    {
        _alertService = alertService;
        _alertHub = alertHub;
        _logger = logger;
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    [HttpPost]
    public async Task<ActionResult<AlertResponseDto>> CreateAlert([FromBody] CreateAlertDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        if (dto.Scope == AlertScope.Radius)
        {
            if (dto.CenterLat == null || dto.CenterLng == null)
            {
                return BadRequest(new { message = "CenterLat and CenterLng are required for Radius scope" });
            }
            if (dto.RadiusKm == null || dto.RadiusKm <= 0)
            {
                return BadRequest(new { message = "Valid RadiusKm is required for Radius scope" });
            }
        }

        var result = await _alertService.CreateAlertAsync(dto, userId.Value);

        try
        {
            await _alertHub.Clients.All.SendAsync("ReceiveAlert", new
            {
                result.AlertId,
                result.Title,
                result.Message,
                Type = result.Type.ToString(),
                Scope = result.Scope.ToString(),
                result.RadiusKm,
                result.CenterLat,
                result.CenterLng,
                result.ExpiresAt,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception hubEx)
        {
            _logger.LogError(hubEx, "Failed to broadcast ReceiveAlert to SignalR hub");
        }

        return CreatedAtAction(nameof(GetAlert), new { id = result.AlertId }, result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AlertResponseDto>> GetAlert(Guid id)
    {
        var alert = await _alertService.GetAlertByIdAsync(id);
        if (alert == null) return NotFound(new { message = "Alert not found" });

        return Ok(alert);
    }

    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<ActionResult<List<AlertListDto>>> GetActiveAlerts()
    {
        var alerts = await _alertService.GetActiveAlertsAsync();
        return Ok(alerts);
    }

    [HttpGet]
    public async Task<ActionResult<List<AlertListDto>>> GetAllAlerts()
    {
        var alerts = await _alertService.GetAllAlertsAsync();
        return Ok(alerts);
    }

    [HttpGet("nearby")]
    [AllowAnonymous]
    public async Task<ActionResult<List<AlertListDto>>> GetNearbyAlerts(
        [FromQuery] double lat,
        [FromQuery] double lng,
        [FromQuery] double radiusKm = 2.0)
    {
        if (lat is < -90 or > 90)
            return BadRequest(new { message = "Latitude must be between -90 and 90." });
        if (lng is < -180 or > 180)
            return BadRequest(new { message = "Longitude must be between -180 and 180." });
        if (radiusKm <= 0 || radiusKm > 50)
            return BadRequest(new { message = "radiusKm must be between 0 and 50." });

        var alerts = await _alertService.GetAlertsForLocationAsync(lat, lng, radiusKm);
        return Ok(alerts);
    }

    [HttpPut("{id}/deactivate")]
    public async Task<ActionResult<AlertResponseDto>> DeactivateAlert(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var result = await _alertService.DeactivateAlertAsync(id, userId.Value);
        if (result == null) return NotFound(new { message = "Alert not found" });

        try
        {
            await _alertHub.Clients.All.SendAsync("AlertDismissed", new
            {
                result.AlertId,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception hubEx)
        {
            _logger.LogError(hubEx, "Failed to broadcast AlertDismissed to SignalR hub");
        }

        return Ok(result);
    }
}
