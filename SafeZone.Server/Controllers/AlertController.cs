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

    public AlertController(
        IAlertService alertService,
        IHubContext<AlertHub> alertHub)
    {
        _alertService = alertService;
        _alertHub = alertHub;
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    [HttpPost]
    public async Task<ActionResult<AlertResponseDto>> CreateAlert(CreateAlertDto dto)
    {
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

        await _alertHub.Clients.All.SendAsync("AlertDismissed", new
        {
            result.AlertId,
            Timestamp = DateTime.UtcNow
        });

        return Ok(result);
    }
}
