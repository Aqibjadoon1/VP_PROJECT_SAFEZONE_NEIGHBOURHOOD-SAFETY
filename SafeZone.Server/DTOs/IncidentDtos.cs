using System.ComponentModel.DataAnnotations;
using SafeZone.Server.Models;

namespace SafeZone.Server.DTOs;

public record CreateIncidentDto
{
    [Required]
    public Guid CategoryId { get; init; }

    [Required]
    [Range(-90, 90)]
    public double Latitude { get; init; }

    [Required]
    [Range(-180, 180)]
    public double Longitude { get; init; }

    [MaxLength(200)]
    public string? Address { get; init; }

    [Required]
    [MaxLength(100)]
    public string Title { get; init; } = string.Empty;

    [Required]
    public string Description { get; init; } = string.Empty;

    public SeverityLevel Severity { get; init; } = SeverityLevel.Medium;

    public bool IsAnonymous { get; init; } = false;

    public DateTime? IncidentDateTime { get; init; }

    public string? EvidenceUrls { get; init; }

    public int? WitnessCount { get; init; }

    [MaxLength(500)]
    public string? SuspectDescription { get; init; }

    public decimal? EstimatedLoss { get; init; }
}

public record UpdateIncidentDto
{
    [MaxLength(100)]
    public string? Title { get; init; }

    public string? Description { get; init; }

    public SeverityLevel? Severity { get; init; }

    public IncidentStatus? Status { get; init; }

    public Guid? AssignedAuthorityId { get; init; }

    public string? ResolutionNotes { get; init; }
}

public record IncidentResponseDto
{
    public Guid IncidentId { get; init; }

    public string IncidentNumber { get; init; } = string.Empty;

    public Guid CategoryId { get; init; }

    public string CategoryName { get; init; } = string.Empty;

    public string CategoryIcon { get; init; } = string.Empty;

    public string CategoryColor { get; init; } = string.Empty;

    public Guid? ReporterId { get; init; }

    public string? ReporterName { get; init; }

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public string Address { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public IncidentStatus Status { get; init; }

    public SeverityLevel Severity { get; init; }

    public bool IsAnonymous { get; init; }

    public bool IsFIRFiled { get; init; }

    public DateTime ReportedAt { get; init; }

    public DateTime? IncidentDateTime { get; init; }

    public DateTime? ResolvedAt { get; init; }

    public Guid? AssignedAuthorityId { get; init; }

    public string? AssignedAuthorityName { get; init; }

    public int? WitnessCount { get; init; }

    public decimal? EstimatedLoss { get; init; }
}

 public record IncidentListDto
{
    public Guid IncidentId { get; init; }

    public string IncidentNumber { get; init; } = string.Empty;

    public string CategoryName { get; init; } = string.Empty;

    public string CategoryIcon { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public IncidentStatus Status { get; init; }

    public SeverityLevel Severity { get; init; }

    public string Address { get; init; } = string.Empty;

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public DateTime ReportedAt { get; init; }

    public DateTime? IncidentDateTime { get; init; }
}

public record MapIncidentDto
{
    public Guid IncidentId { get; init; }

    public string IncidentNumber { get; init; } = string.Empty;

    public double Lat { get; init; }

    public double Lng { get; init; }

    public string Title { get; init; } = string.Empty;

    public string CategoryName { get; init; } = string.Empty;

    public string CategoryIcon { get; init; } = string.Empty;

    public string CategoryColor { get; init; } = string.Empty;

    public IncidentStatus Status { get; init; }

    public SeverityLevel Severity { get; init; }

    public DateTime ReportedAt { get; init; }
}

public record HeatmapPointDto
{
    public double Lat { get; init; }

    public double Lng { get; init; }

    public double Intensity { get; init; }

    public SeverityLevel Severity { get; init; }
}

public record CategoryDto
{
    public Guid CategoryId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Icon { get; init; } = string.Empty;

    public string Color { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;
}
