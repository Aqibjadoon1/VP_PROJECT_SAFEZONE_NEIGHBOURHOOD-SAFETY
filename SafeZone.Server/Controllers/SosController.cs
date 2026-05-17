using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SafeZone.Server.DTOs;
using SafeZone.Server.Models;
using SafeZone.Server.Services;

namespace SafeZone.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SosController : ControllerBase
{
    private readonly ISosService _sosService;

    public SosController(ISosService sosService)
    {
        _sosService = sosService;
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    [HttpPost("trigger")]
    public async Task<ActionResult<SosResponseDto>> TriggerEmergency(TriggerSosDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var result = await _sosService.TriggerEmergencyAsync(dto, userId.Value);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("my-logs")]
    public async Task<ActionResult<List<SosCallLogDto>>> GetMyLogs()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var logs = await _sosService.GetMyCallLogsAsync(userId.Value);
        return Ok(logs);
    }

    [HttpGet("logs")]
    [Authorize(Roles = "Authority,SuperAdmin")]
    public async Task<ActionResult<List<SosCallLogDto>>> GetAllLogs([FromQuery] int? status)
    {
        CallStatus? statusEnum = null;
        if (status.HasValue)
        {
            statusEnum = (CallStatus)status.Value;
        }

        var logs = await _sosService.GetAllCallLogsAsync(statusEnum);
        return Ok(logs);
    }

    [HttpGet("logs/{id}")]
    public async Task<ActionResult<SosCallLogDto>> GetLogById(Guid id)
    {
        var log = await _sosService.GetCallLogByIdAsync(id);
        if (log == null) return NotFound(new { message = "Call log not found" });

        var userId = GetCurrentUserId();
        var userRole = User.FindFirstValue(ClaimTypes.Role);

        if (userRole != "Authority" && userRole != "SuperAdmin")
        {
            var myLogs = await _sosService.GetMyCallLogsAsync(userId ?? Guid.Empty);
            if (!myLogs.Any(l => l.LogId == id))
            {
                return Forbid();
            }
        }

        return Ok(log);
    }

    [HttpPut("logs/{id}/false-alarm")]
    public async Task<ActionResult<SosCallLogDto>> MarkAsFalseAlarm(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var result = await _sosService.MarkAsFalseAlarmAsync(id, userId.Value);
        if (result == null) return NotFound(new { message = "Call log not found or not authorized" });

        return Ok(result);
    }

    [HttpGet("status")]
    public ActionResult<object> GetStatus()
    {
        return Ok(new
        {
            isMockMode = _sosService.IsMockMode,
            emergencyTypes = Enum.GetValues<AuthorityType>().Select(t => new
            {
                type = t,
                name = t.ToString(),
                number = t switch
                {
                    AuthorityType.Police => "15",
                    AuthorityType.Ambulance => "115",
                    AuthorityType.FireBrigade => "16",
                    AuthorityType.TrafficPolice => "1915",
                    _ => "15"
                }
            }),
            mockModeNote = _sosService.IsMockMode 
                ? "Running in mock mode. No actual calls will be made. Configure Twilio and OpenAI API keys for production use."
                : "Production mode - actual calls will be made"
        });
    }
}
