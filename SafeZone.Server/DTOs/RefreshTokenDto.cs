using System.ComponentModel.DataAnnotations;

namespace SafeZone.Server.DTOs;

public class RefreshTokenDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
