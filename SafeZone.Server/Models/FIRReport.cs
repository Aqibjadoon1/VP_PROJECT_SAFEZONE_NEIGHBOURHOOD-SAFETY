using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SafeZone.Server.Models;

public class FIRReport
{
    [Key]
    public Guid FIRId { get; set; } = Guid.NewGuid();
    
    [MaxLength(30)]
    public string FIRNumber { get; set; } = string.Empty;
    
    public Guid IncidentId { get; set; }
    
    public Guid ReporterId { get; set; }
    
    [MaxLength(100)]
    public string ComplainantName { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string ComplainantCNIC { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string? ComplainantPhone { get; set; }
    
    [MaxLength(200)]
    public string? ComplainantAddress { get; set; }
    
    [MaxLength(50)]
    public string? ComplainantFatherName { get; set; }
    
    public DateTime? ComplainantDateOfBirth { get; set; }
    
    [MaxLength(500)]
    public string? AccusedDescription { get; set; }
    
    public string IncidentNarrative { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? WitnessDetails { get; set; }
    
    [MaxLength(200)]
    public string? PropertyLost { get; set; }
    
    public double EstimatedLoss { get; set; }
    
    public FIRStatus Status { get; set; } = FIRStatus.Submitted;
    
    [MaxLength(500)]
    public string? RejectionReason { get; set; }
    
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ReviewedAt { get; set; }
    
    public Guid? ReviewedByAuthorityId { get; set; }
    
    [MaxLength(255)]
    public string? PDFUrl { get; set; }
    
    public DateTime IncidentDateTime { get; set; }
    
    [MaxLength(200)]
    public string IncidentPlace { get; set; } = string.Empty;
    
    public double IncidentLatitude { get; set; }
    
    public double IncidentLongitude { get; set; }
    
    public int NumberOfAccused { get; set; }
    
    public bool AccusedKnown { get; set; }
    
    [MaxLength(100)]
    public string? AccusedName { get; set; }
    
    [MaxLength(50)]
    public string? AccusedCNIC { get; set; }
    
    [MaxLength(200)]
    public string? AccusedAddress { get; set; }
    
    public byte[]? DigitalSignature { get; set; }
    
    public bool DeclarationAccepted { get; set; }

    [ForeignKey(nameof(IncidentId))]
    public Incident Incident { get; set; } = null!;
    
     [ForeignKey(nameof(ReporterId))]
    public User Reporter { get; set; } = null!;
    
    [ForeignKey(nameof(ReviewedByAuthorityId))]
    public User? ReviewedByAuthority { get; set; }
}
