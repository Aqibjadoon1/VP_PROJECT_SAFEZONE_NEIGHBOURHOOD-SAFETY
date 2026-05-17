using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SafeZone.Server.DTOs;
using SafeZone.Server.Services;

namespace SafeZone.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VoiceCallController : ControllerBase
{
    private readonly IVoiceCallService _voiceCallService;

    public VoiceCallController(IVoiceCallService voiceCallService)
    {
        _voiceCallService = voiceCallService;
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    [HttpPost("start")]
    [Authorize(Roles = "Authority,SuperAdmin")]
    public async Task<ActionResult<CallResponseDto>> StartCall(StartCallDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.PhoneNumber))
        {
            return BadRequest(new { message = "Phone number is required" });
        }

        var userId = GetCurrentUserId();

        try
        {
            var session = await _voiceCallService.StartOutboundCallAsync(
                dto.PhoneNumber,
                dto.SystemPrompt,
                userId);

            return Ok(new CallResponseDto
            {
                CallId = session.CallId,
                RemoteNumber = session.RemoteNumber,
                Status = session.Status.ToString(),
                Direction = session.Direction.ToString(),
                CreatedAt = session.CreatedAt,
                IsMockMode = _voiceCallService.IsMockMode
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("active")]
    [Authorize(Roles = "Authority,SuperAdmin")]
    public async Task<ActionResult<List<CallResponseDto>>> GetActiveCalls()
    {
        var sessions = await _voiceCallService.GetActiveCallsAsync();

        var result = sessions.Select(s => new CallResponseDto
        {
            CallId = s.CallId,
            RemoteNumber = s.RemoteNumber,
            Status = s.Status.ToString(),
            Direction = s.Direction.ToString(),
            CreatedAt = s.CreatedAt,
            IsMockMode = _voiceCallService.IsMockMode
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{callId}")]
    public async Task<ActionResult<CallResponseDto>> GetCall(Guid callId)
    {
        var session = await _voiceCallService.GetCallAsync(callId);
        if (session == null)
        {
            return NotFound(new { message = "Call not found" });
        }

        var userRole = User.FindFirstValue(ClaimTypes.Role);
        if (userRole != "Authority" && userRole != "SuperAdmin")
        {
            var userId = GetCurrentUserId();
            if (session.TriggeredByUserId != userId)
            {
                return Forbid();
            }
        }

        return Ok(new CallResponseDto
        {
            CallId = session.CallId,
            RemoteNumber = session.RemoteNumber,
            Status = session.Status.ToString(),
            Direction = session.Direction.ToString(),
            CreatedAt = session.CreatedAt,
            IsMockMode = _voiceCallService.IsMockMode
        });
    }

    [HttpGet("{callId}/transcript")]
    public async Task<ActionResult<List<TranscriptSegmentDto>>> GetTranscript(Guid callId)
    {
        var session = await _voiceCallService.GetCallAsync(callId);
        if (session == null)
        {
            return NotFound(new { message = "Call not found" });
        }

        var userRole = User.FindFirstValue(ClaimTypes.Role);
        if (userRole != "Authority" && userRole != "SuperAdmin")
        {
            var userId = GetCurrentUserId();
            if (session.TriggeredByUserId != userId)
            {
                return Forbid();
            }
        }

        var transcript = session.Transcript.Select(t => new TranscriptSegmentDto
        {
            Speaker = t.Speaker.ToString(),
            Text = t.Text,
            Timestamp = t.Timestamp
        }).ToList();

        return Ok(transcript);
    }

    [HttpPost("{callId}/end")]
    [Authorize(Roles = "Authority,SuperAdmin")]
    public async Task<IActionResult> EndCall(Guid callId, [FromBody] string? reason = null)
    {
        var session = await _voiceCallService.GetCallAsync(callId);
        if (session == null)
        {
            return NotFound(new { message = "Call not found" });
        }

        await _voiceCallService.EndCallAsync(callId, reason);

        return Ok(new { message = "Call ended", callId });
    }

    [HttpGet("status")]
    public ActionResult<object> GetStatus()
    {
        return Ok(new
        {
            isMockMode = _voiceCallService.IsMockMode,
            mockModeNote = _voiceCallService.IsMockMode
                ? "Running in mock mode. Voice pipeline uses mock providers. Add API keys for real functionality."
                : "Production mode - real providers configured"
        });
    }
}
