using System.ComponentModel.DataAnnotations;

namespace SafeZone.Server.Models;

public class Alert
{
    [Key]
    public Guid AlertId { get; set; } = Guid.NewGuid();
    
    public Guid IssuedByAuthorityId { get; set; }
    
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;
    
    public string Message { get; set; } = string.Empty;
    
    public AlertType Type { get; set; }
    
    public AlertScope Scope { get; set; }
    
    public double? RadiusKm { get; set; }
    
    public double? CenterLat { get; set; }
    
    public double? CenterLng { get; set; }
    
    public string? TargetGeoJson { get; set; }
    
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ExpiresAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime? ScheduledAt { get; set; }
}
