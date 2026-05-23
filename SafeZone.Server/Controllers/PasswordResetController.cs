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

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        try
        {
            var token = await _authService.GeneratePasswordResetTokenAsync(dto.PhoneNumber);
            return Ok(new { success = true, message = "Password reset token generated.", token });
        }
        catch (KeyNotFoundException)
        {
            return Ok(new { success = true, message = "If the phone number exists, a reset token has been generated." });
        }
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        var result = await _authService.ResetPasswordAsync(dto.PhoneNumber, dto.Token, dto.NewPassword);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}
