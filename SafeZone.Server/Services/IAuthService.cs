using SafeZone.Server.DTOs;
using SafeZone.Server.Models;

namespace SafeZone.Server.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
    Task<AuthResponseDto> LogoutAsync(Guid userId);
    Task<UserDto?> GetUserByIdAsync(Guid userId);
    Task<string> GenerateJwtTokenAsync(User user);
    string GenerateRefreshToken();
}
