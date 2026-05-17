using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SafeZone.Server.Models;

public class AICallLog
{
    [Key]
    public Guid LogId { get; set; } = Guid.NewGuid();
    
    public Guid IncidentId { get; set; }
    
    public Guid TriggeredByUserId { get; set; }
    
    [MaxLength(50)]
    public string CallType { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string PhoneNumberCalled { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? CalledNumbers { get; set; }
    
    [MaxLength(100)]
    public string? TwilioCallSid { get; set; }
    
    public string AIScript { get; set; } = string.Empty;
    
    public CallStatus Status { get; set; } = CallStatus.Initiated;
    
    public DateTime InitiatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? CompletedAt { get; set; }
    
    public int DurationSeconds { get; set; }
    
    [MaxLength(255)]
    public string? TranscriptUrl { get; set; }
    
    [MaxLength(50)]
    public string? SmsStatus { get; set; }
    
    public bool IsFalseAlarm { get; set; }

    [ForeignKey(nameof(IncidentId))]
    public Incident Incident { get; set; } = null!;
}
