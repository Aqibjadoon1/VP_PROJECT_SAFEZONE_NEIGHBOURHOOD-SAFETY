using System.ComponentModel.DataAnnotations;
using SafeZone.Server.Models;

namespace SafeZone.Server.DTOs;

public record CreateAlertDto
{
    [Required]
    [MaxLength(100)]
    public string Title { get; init; } = string.Empty;

    [Required]
    public string Message { get; init; } = string.Empty;

    public AlertType Type { get; init; } = AlertType.Info;

    public AlertScope Scope { get; init; } = AlertScope.Citywide;

    public double? RadiusKm { get; init; }

    public double? CenterLat { get; init; }

    public double? CenterLng { get; init; }

    public int? ExpiresInMinutes { get; init; }

    public DateTime? ScheduledAt { get; init; }
}

public record AlertResponseDto
{
    public Guid AlertId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public AlertType Type { get; init; }

    public AlertScope Scope { get; init; }

    public double? RadiusKm { get; init; }

    public double? CenterLat { get; init; }

    public double? CenterLng { get; init; }

    public DateTime IssuedAt { get; init; }

    public DateTime? ExpiresAt { get; init; }

    public bool IsActive { get; init; }

    public Guid IssuedByAuthorityId { get; init; }

    public string? IssuedByName { get; init; }
}

public record AlertListDto
{
    public Guid AlertId { get; init; }

    public string Title { get; init; } = string.Empty;

    public AlertType Type { get; init; }

    public AlertScope Scope { get; init; }

    public DateTime IssuedAt { get; init; }

    public DateTime? ExpiresAt { get; init; }

    public bool IsActive { get; init; }
}
