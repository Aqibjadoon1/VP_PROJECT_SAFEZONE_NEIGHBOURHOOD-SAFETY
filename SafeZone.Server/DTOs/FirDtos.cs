using System.ComponentModel.DataAnnotations;
using SafeZone.Server.Models;

namespace SafeZone.Server.DTOs;

public record CreateFirDto
{
    public Guid? IncidentId { get; init; }

    [Required]
    [MaxLength(100)]
    public string ComplainantName { get; init; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string ComplainantCNIC { get; init; } = string.Empty;

    [MaxLength(50)]
    public string? ComplainantPhone { get; init; }

    [MaxLength(200)]
    public string? ComplainantAddress { get; init; }

    [MaxLength(50)]
    public string? ComplainantFatherName { get; init; }

    public DateTime? ComplainantDateOfBirth { get; init; }

    [MaxLength(500)]
    public string? AccusedDescription { get; init; }

    [Required]
    public string IncidentNarrative { get; init; } = string.Empty;

    [MaxLength(500)]
    public string? WitnessDetails { get; init; }

    [MaxLength(200)]
    public string? PropertyLost { get; init; }

    public double EstimatedLoss { get; init; }

    public DateTime IncidentDateTime { get; init; }

    [Required]
    [MaxLength(200)]
    public string IncidentPlace { get; init; } = string.Empty;

    public double IncidentLatitude { get; init; }

    public double IncidentLongitude { get; init; }

    public int NumberOfAccused { get; init; }

    public bool AccusedKnown { get; init; }

    [MaxLength(100)]
    public string? AccusedName { get; init; }

    [MaxLength(50)]
    public string? AccusedCNIC { get; init; }

    [MaxLength(200)]
    public string? AccusedAddress { get; init; }

    public bool DeclarationAccepted { get; init; }
}

public record FirResponseDto
{
    public Guid FirId { get; init; }

    public string FirNumber { get; init; } = string.Empty;

    public Guid IncidentId { get; init; }

    public string? IncidentTitle { get; init; }

    public Guid ReporterId { get; init; }

    public string? ReporterName { get; init; }

    public string ComplainantName { get; init; } = string.Empty;

    public string ComplainantCNIC { get; init; } = string.Empty;

    public string? ComplainantPhone { get; init; }

    public string? ComplainantAddress { get; init; }

    public string? ComplainantFatherName { get; init; }

    public DateTime? ComplainantDateOfBirth { get; init; }

    public string? AccusedDescription { get; init; }

    public string IncidentNarrative { get; init; } = string.Empty;

    public string? WitnessDetails { get; init; }

    public string? PropertyLost { get; init; }

    public double EstimatedLoss { get; init; }

    public FIRStatus Status { get; init; }

    public string? RejectionReason { get; init; }

    public DateTime SubmittedAt { get; init; }

    public DateTime? ReviewedAt { get; init; }

    public Guid? ReviewedByAuthorityId { get; init; }

    public string? ReviewedByName { get; init; }

    public string? PdfUrl { get; init; }

    public DateTime IncidentDateTime { get; init; }

    public string IncidentPlace { get; init; } = string.Empty;

    public double IncidentLatitude { get; init; }

    public double IncidentLongitude { get; init; }

    public int NumberOfAccused { get; init; }

    public bool AccusedKnown { get; init; }

    public string? AccusedName { get; init; }

    public string? AccusedCNIC { get; init; }

    public string? AccusedAddress { get; init; }

    public bool DeclarationAccepted { get; init; }
}

public record FirListDto
{
    public Guid FirId { get; init; }

    public string FirNumber { get; init; } = string.Empty;

    public Guid IncidentId { get; init; }

    public string? IncidentTitle { get; init; }

    public string ComplainantName { get; init; } = string.Empty;

    public FIRStatus Status { get; init; }

    public DateTime SubmittedAt { get; init; }

    public DateTime? ReviewedAt { get; init; }

    public string? ReviewedByName { get; init; }
}

public record ReviewFirDto
{
    public FIRStatus Status { get; init; }

    [MaxLength(500)]
    public string? RejectionReason { get; init; }
}
