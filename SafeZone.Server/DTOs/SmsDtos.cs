using System.ComponentModel.DataAnnotations;

namespace SafeZone.Server.DTOs;

public record SendSmsDto
{
    [Required]
    [Phone]
    public string ToNumber { get; init; } = string.Empty;

    [Required]
    [MaxLength(1600)]
    public string Message { get; init; } = string.Empty;
}

public record SendBulkSmsDto
{
    [Required]
    public List<string> ToNumbers { get; init; } = new();

    [Required]
    [MaxLength(1600)]
    public string Message { get; init; } = string.Empty;
}
