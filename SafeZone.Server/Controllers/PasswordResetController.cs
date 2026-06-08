using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeZone.Server.DTOs;
using SafeZone.Server.Services;

namespace SafeZone.Server.Controllers;

[ApiController]
[Route("api/auth")]
public class PasswordResetController : ControllerBase
{
    private readonly IAuthService _authService;

    public PasswordResetController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Invalid request." });
        }

        // Always return the same response to prevent user enumeration
        string responseMessage = "If the phone number exists, a reset token has been sent.";

        try
        {
            _ = await _authService.GeneratePasswordResetTokenAsync(dto.PhoneNumber);
        }
        catch (KeyNotFoundException)
        {
            // User does not exist — do not reveal this
        }

        return Ok(new { success = true, message = responseMessage });
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Invalid request." });
        }

        var result = await _authService.ResetPasswordAsync(dto.PhoneNumber, dto.Token, dto.NewPassword);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}
