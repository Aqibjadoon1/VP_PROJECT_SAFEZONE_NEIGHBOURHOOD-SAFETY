using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SafeZone.Server.Data;
using SafeZone.Server.DTOs;
using SafeZone.Server.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SafeZone.Server.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly SafeZoneDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService>? _logger;

    public AuthService(
        UserManager<User> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        SafeZoneDbContext context,
        IConfiguration configuration,
        ILogger<AuthService>? logger = null)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        var phone = dto.PhoneNumber?.Trim() ?? string.Empty;
        var email = dto.Email?.Trim();

        if (string.IsNullOrWhiteSpace(phone))
        {
            return new AuthResponseDto { Success = false, Message = "Phone number is required." };
        }

        if (string.IsNullOrWhiteSpace(dto.FullName))
        {
            return new AuthResponseDto { Success = false, Message = "Full name is required." };
        }

        if (dto.Password != dto.ConfirmPassword)
        {
            return new AuthResponseDto { Success = false, Message = "Passwords do not match." };
        }

        var existingUser = await _userManager.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == phone);

        if (existingUser != null)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Phone number is already registered."
            };
        }

        var user = new User
        {
            UserName = phone,
            PhoneNumber = phone,
            FullName = dto.FullName?.Trim() ?? string.Empty,
            Role = dto.Role,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            Email = email
        };

        IdentityResult result;
        try
        {
            result = await _userManager.CreateAsync(user, dto.Password);
        }
        catch (DbUpdateException ex)
        {
            _logger?.LogError(ex, "Database error while registering phone {Phone}.", phone);
            return new AuthResponseDto
            {
                Success = false,
                Message = "Registration failed because the account could not be saved. Please try again."
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error while registering phone {Phone}.", phone);
            return new AuthResponseDto
            {
                Success = false,
                Message = "Registration failed unexpectedly. Please try again."
            };
        }

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return new AuthResponseDto
            {
                Success = false,
                Message = $"Registration failed: {errors}"
            };
        }

        var roleName = dto.Role.ToString();
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            var roleCreateResult = await _roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            if (!roleCreateResult.Succeeded)
            {
                _logger?.LogError(
                    "Role creation failed for {Role}: {Errors}",
                    roleName,
                    string.Join(", ", roleCreateResult.Errors.Select(e => e.Description)));
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Account was created, but role setup failed. Contact an administrator."
                };
            }
        }
        var roleResult = await _userManager.AddToRoleAsync(user, roleName);
        if (!roleResult.Succeeded)
        {
            _logger?.LogError(
                "Role assignment failed for user {UserId}: {Errors}",
                user.Id,
                string.Join(", ", roleResult.Errors.Select(e => e.Description)));
            return new AuthResponseDto
            {
                Success = false,
                Message = "Account was created, but role assignment failed. Contact an administrator."
            };
        }

        string? token = null;
        string? refreshToken = null;
        DateTime? expiresAt = null;
        var message = "Account created successfully.";

        try
        {
            token = await GenerateJwtTokenAsync(user);
            refreshToken = GenerateRefreshToken();
            expiresAt = DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("Jwt:ExpiryMinutes", 15));

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                _logger?.LogWarning(
                    "Refresh token update failed for user {UserId}: {Errors}",
                    user.Id,
                    string.Join(", ", updateResult.Errors.Select(e => e.Description)));
                refreshToken = null;
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            _logger?.LogWarning(ex, "Account {UserId} created, but JWT token could not be issued.", user.Id);
            message = "Account created successfully. Please log in.";
        }

        return new AuthResponseDto
        {
            Success = true,
            Message = message,
            Token = token,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            User = MapToUserDto(user)
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var login = dto.Identifier?.Trim() ?? string.Empty;
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == login || u.Email == login || u.UserName == login);

        if (user == null)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Invalid credentials. Check your phone, email, or password."
            };
        }

        if (!user.IsActive)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Account is deactivated."
            };
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Account is locked out due to too many failed attempts. Try again later."
            };
        }

        var result = await _userManager.CheckPasswordAsync(user, dto.Password);

        if (!result)
        {
            await _userManager.AccessFailedAsync(user);
            return new AuthResponseDto
            {
                Success = false,
                Message = "Invalid phone number or password."
            };
        }

        await _userManager.ResetAccessFailedCountAsync(user);
        user.LastActiveAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var token = await GenerateJwtTokenAsync(user);
        var refreshToken = GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _userManager.UpdateAsync(user);

        return new AuthResponseDto
        {
            Success = true,
            Message = "Login successful.",
            Token = token,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("Jwt:ExpiryMinutes", 15)),
            User = MapToUserDto(user)
        };
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return new AuthResponseDto { Success = false, Message = "Refresh token is required." };
        }

        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

        if (user == null)
        {
            return new AuthResponseDto { Success = false, Message = "Invalid refresh token." };
        }

        if (user.RefreshTokenExpiry is null || user.RefreshTokenExpiry < DateTime.UtcNow)
        {
            return new AuthResponseDto { Success = false, Message = "Refresh token has expired." };
        }

        if (!user.IsActive)
        {
            return new AuthResponseDto { Success = false, Message = "Account is deactivated." };
        }

        var newToken = await GenerateJwtTokenAsync(user);
        var newRefreshToken = GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        user.LastActiveAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return new AuthResponseDto
        {
            Success = true,
            Message = "Token refreshed.",
            Token = newToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("Jwt:ExpiryMinutes", 15)),
            User = MapToUserDto(user)
        };
    }

    public async Task<AuthResponseDto> LogoutAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user != null)
        {
            user.LastActiveAt = DateTime.UtcNow;
            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            await _userManager.UpdateAsync(user);
        }

        return new AuthResponseDto
        {
            Success = true,
            Message = "Logout successful."
        };
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user != null ? MapToUserDto(user) : null;
    }

    public async Task<string> GenerateJwtTokenAsync(User user)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured.");
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured.");
        var jwtAudience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured.");
        var expiryMinutes = _configuration.GetValue<int>("Jwt:ExpiryMinutes", 15);

        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName ?? user.PhoneNumber ?? string.Empty),
            new Claim(ClaimTypes.MobilePhone, user.PhoneNumber ?? string.Empty),
            new Claim("FullName", user.FullName ?? string.Empty),
            new Claim("Role", user.Role.ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private static UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            FullName = user.FullName ?? string.Empty,
            Role = user.Role,
            LastKnownLatitude = user.LastKnownLatitude,
            LastKnownLongitude = user.LastKnownLongitude,
            ProximityRadiusKm = user.ProximityRadiusKm,
            IsAnonymous = user.IsAnonymous,
            PushNotificationsEnabled = user.PushNotificationsEnabled,
            CreatedAt = user.CreatedAt,
            LastActiveAt = user.LastActiveAt,
            IsActive = user.IsActive
        };
    }

    public async Task<string> GeneratePasswordResetTokenAsync(string phoneNumber)
    {
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);

        if (user == null)
            throw new KeyNotFoundException("User not found.");

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        return token;
    }

    public async Task<AuthResponseDto> ResetPasswordAsync(string phoneNumber, string token, string newPassword)
    {
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);

        if (user == null)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "User not found."
            };
        }

        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return new AuthResponseDto
            {
                Success = false,
                Message = $"Password reset failed: {errors}"
            };
        }

        return new AuthResponseDto
        {
            Success = true,
            Message = "Password has been reset successfully."
        };
    }
}
