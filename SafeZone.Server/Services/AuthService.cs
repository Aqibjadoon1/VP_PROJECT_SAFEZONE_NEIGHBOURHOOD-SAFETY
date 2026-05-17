using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

    public AuthService(
        UserManager<User> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        SafeZoneDbContext context,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _configuration = configuration;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        var existingUser = await _userManager.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == dto.PhoneNumber);

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
            UserName = dto.PhoneNumber,
            PhoneNumber = dto.PhoneNumber,
            FullName = dto.FullName,
            Role = dto.Role,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            Email = null
        };

        var result = await _userManager.CreateAsync(user, dto.Password);

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
            await _roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
        }
        await _userManager.AddToRoleAsync(user, roleName);

        var token = await GenerateJwtTokenAsync(user);
        var refreshToken = GenerateRefreshToken();

        return new AuthResponseDto
        {
            Success = true,
            Message = "Registration successful.",
            Token = token,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("Jwt:ExpiryMinutes", 15)),
            User = MapToUserDto(user)
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == dto.PhoneNumber);

        if (user == null)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Invalid phone number or password."
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

        var result = await _userManager.CheckPasswordAsync(user, dto.Password);

        if (!result)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Invalid phone number or password."
            };
        }

        user.LastActiveAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var token = await GenerateJwtTokenAsync(user);
        var refreshToken = GenerateRefreshToken();

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
        return await Task.FromResult(new AuthResponseDto
        {
            Success = false,
            Message = "Refresh token endpoint is simplified for this version. Please login again."
        });
    }

    public async Task<AuthResponseDto> LogoutAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user != null)
        {
            user.LastActiveAt = DateTime.UtcNow;
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
            new Claim("FullName", user.FullName),
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
            FullName = user.FullName,
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
}
