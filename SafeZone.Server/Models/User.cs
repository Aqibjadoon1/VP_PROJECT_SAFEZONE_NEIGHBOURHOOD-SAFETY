using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace SafeZone.Server.Models;

public class User : IdentityUser<Guid>
{
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;
    
    public UserRole Role { get; set; } = UserRole.Resident;
    
    [MaxLength(100)]
    public string? PhoneHash { get; set; }
    
    public double? LastKnownLatitude { get; set; }
    
    public double? LastKnownLongitude { get; set; }
    
    public double ProximityRadiusKm { get; set; } = 2.0;
    
    public bool IsAnonymous { get; set; } = false;
    
    public bool PushNotificationsEnabled { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastActiveAt { get; set; }
    
    public bool IsActive { get; set; } = true;

    [MaxLength(512)]
    public string? RefreshToken { get; set; }

    public DateTime? RefreshTokenExpiry { get; set; }

    public Authority? AuthorityProfile { get; set; }
    public ICollection<Incident> ReportedIncidents { get; set; } = new List<Incident>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<FIRReport> FIRReports { get; set; } = new List<FIRReport>();
}
