using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SafeZone.Server.Models;

public class Incident
{
    [Key]
    public Guid IncidentId { get; set; } = Guid.NewGuid();
    
    [MaxLength(30)]
    public string IncidentNumber { get; set; } = string.Empty;
    
    public Guid CategoryId { get; set; }
    
    public Guid? ReporterId { get; set; }
    
    public double Latitude { get; set; }
    
    public double Longitude { get; set; }
    
    [MaxLength(200)]
    public string Address { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public IncidentStatus Status { get; set; } = IncidentStatus.Pending;
    
    public SeverityLevel Severity { get; set; } = SeverityLevel.Medium;
    
    public bool IsAnonymous { get; set; } = false;
    
    public bool IsFIRFiled { get; set; } = false;
    
    public string? EvidenceUrls { get; set; }
    
    public DateTime ReportedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? IncidentDateTime { get; set; }
    
    public DateTime? ResolvedAt { get; set; }
    
    public Guid? AssignedAuthorityId { get; set; }
    
    [MaxLength(500)]
    public string? AIGeneratedSummary { get; set; }
    
    [MaxLength(100)]
    public string? AICallLogId { get; set; }
    
    public int? WitnessCount { get; set; }
    
    [MaxLength(500)]
    public string? SuspectDescription { get; set; }
    
    public decimal? EstimatedLoss { get; set; }
    
    [MaxLength(50)]
    public string? SubCategory { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public IncidentCategory Category { get; set; } = null!;
    
    [ForeignKey(nameof(ReporterId))]
    public User? Reporter { get; set; }
    
    public FIRReport? FIR { get; set; }
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Response> Responses { get; set; } = new List<Response>();
}
