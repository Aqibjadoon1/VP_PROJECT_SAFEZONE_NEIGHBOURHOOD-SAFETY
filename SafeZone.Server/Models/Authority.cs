using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SafeZone.Server.Models;

public class Authority
{
    [Key]
    public Guid AuthId { get; set; } = Guid.NewGuid();
    
    public Guid UserId { get; set; }
    
    [MaxLength(100)]
    public string UnitName { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string? BadgeNumber { get; set; }
    
    public string? JurisdictionGeoJson { get; set; }
    
    public double JurisdictionCenterLat { get; set; }
    
    public double JurisdictionCenterLng { get; set; }
    
    [MaxLength(200)]
    public string? ContactInfo { get; set; }
    
    [MaxLength(50)]
    public string? EmergencyPhone { get; set; }
    
    public bool IsOnDuty { get; set; }
    
    public AuthorityType Type { get; set; }
    
    [MaxLength(50)]
    public string? Rank { get; set; }
    
    [MaxLength(50)]
    public string? Department { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
    
    public ICollection<Response> Responses { get; set; } = new List<Response>();
}
