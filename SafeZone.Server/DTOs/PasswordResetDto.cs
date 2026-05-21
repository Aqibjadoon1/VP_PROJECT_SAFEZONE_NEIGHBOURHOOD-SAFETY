using System.ComponentModel.DataAnnotations;

namespace SafeZone.Server.DTOs;

public class ForgotPasswordDto
{
    [Required]
    public string PhoneNumber { get; set; } = string.Empty;
}

public class ResetPasswordDto
{
    [Required]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;
}
