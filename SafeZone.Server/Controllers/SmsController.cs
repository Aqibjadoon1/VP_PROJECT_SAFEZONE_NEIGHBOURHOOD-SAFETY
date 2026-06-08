using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeZone.Server.DTOs;
using SafeZone.Server.Services;

namespace SafeZone.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SmsController : ControllerBase
{
    private readonly ISmsService _smsService;

    public SmsController(ISmsService smsService)
    {
        _smsService = smsService;
    }

    [HttpPost("send")]
    [Authorize(Roles = "Authority,SuperAdmin")]
    public async Task<ActionResult<SmsResult>> SendSms([FromBody] SendSmsDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (string.IsNullOrWhiteSpace(dto.ToNumber))
        {
            return BadRequest(new { message = "Phone number is required" });
        }

        if (string.IsNullOrWhiteSpace(dto.Message))
        {
            return BadRequest(new { message = "Message is required" });
        }

        try
        {
            var result = await _smsService.SendSmsAsync(dto.ToNumber, dto.Message);
            return Ok(result);
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Failed to send SMS." });
        }
    }

    [HttpPost("bulk")]
    [Authorize(Roles = "Authority,SuperAdmin")]
    public async Task<ActionResult<List<SmsResult>>> SendBulkSms([FromBody] SendBulkSmsDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (dto.ToNumbers == null || dto.ToNumbers.Count == 0)
        {
            return BadRequest(new { message = "At least one phone number is required" });
        }

        const int maxRecipients = 100;
        if (dto.ToNumbers.Count > maxRecipients)
        {
            return BadRequest(new { message = $"Bulk SMS is limited to {maxRecipients} recipients per request." });
        }

        if (string.IsNullOrWhiteSpace(dto.Message))
        {
            return BadRequest(new { message = "Message is required" });
        }

        try
        {
            var results = await _smsService.SendBulkSmsAsync(dto.ToNumbers, dto.Message);
            return Ok(results);
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Failed to send bulk SMS." });
        }
    }

    [HttpGet("status")]
    public ActionResult<object> GetStatus()
    {
        return Ok(new
        {
            isMockMode = _smsService.IsMockMode,
            mockModeNote = _smsService.IsMockMode
                ? "Running in mock mode. SMS messages are simulated. Configure Twilio API keys for production use."
                : "Production mode - real SMS will be sent via Twilio"
        });
    }
}
