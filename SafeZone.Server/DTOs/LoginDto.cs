using System.ComponentModel.DataAnnotations;

namespace SafeZone.Server.DTOs;

public class LoginDto
{
    [Required]
    public string Identifier { get; set; } = string.Empty;

    [Required]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    public string Password { get; set; } = string.Empty;
}
