using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeZone.Server.DTOs;
using SafeZone.Server.Services;
using System.Security.Claims;

namespace SafeZone.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>Register a new user account with phone number and password.</summary>
    /// <remarks>Creates user with specified role (Resident/Authority). Returns JWT token and refresh token.</remarks>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(kvp => kvp.Value?.Errors.Count > 0)
                .SelectMany(kvp => kvp.Value!.Errors.Select(error => $"{kvp.Key}: {error.ErrorMessage}"));

            return BadRequest(new AuthResponseDto
            {
                Success = false,
                Message = $"Invalid registration data. {string.Join(" ", errors)}"
            });
        }

        try
        {
            var result = await _authService.RegisterAsync(dto);
            if (!result.Success)
            {
                _logger.LogWarning("Registration failed for {Phone}: {Message}", dto.PhoneNumber, result.Message);
                return BadRequest(result);
            }

            _logger.LogInformation("Registration succeeded for {Phone}. TokenIssued={TokenIssued}", dto.PhoneNumber, !string.IsNullOrWhiteSpace(result.Token));
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled API registration error for {Phone}.", dto.PhoneNumber);
            return StatusCode(500, new AuthResponseDto
            {
                Success = false,
                Message = "Registration failed because of a server error. Please try again."
            });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new AuthResponseDto
            {
                Success = false,
                Message = "Invalid login data."
            });
        }

        var result = await _authService.LoginAsync(dto);
        if (!result.Success)
        {
            return Unauthorized(result);
        }

        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new AuthResponseDto
            {
                Success = false,
                Message = "Invalid refresh token."
            });
        }

        var result = await _authService.RefreshTokenAsync(dto.RefreshToken);
        if (!result.Success)
        {
            return Unauthorized(result);
        }

        return Ok(result);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            return Unauthorized(new AuthResponseDto
            {
                Success = false,
                Message = "User not authenticated."
            });
        }

        var userId = Guid.Parse(userIdClaim.Value);
        var result = await _authService.LogoutAsync(userId);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            return Unauthorized(new
            {
                Success = false,
                Message = "User not authenticated."
            });
        }

        var userId = Guid.Parse(userIdClaim.Value);
        var user = await _authService.GetUserByIdAsync(userId);

        if (user == null)
        {
            return NotFound(new
            {
                Success = false,
                Message = "User not found."
            });
        }

        return Ok(new
        {
            Success = true,
            User = user
        });
    }
}
