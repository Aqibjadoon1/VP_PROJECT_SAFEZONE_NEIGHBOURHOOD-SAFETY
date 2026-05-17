using System.ComponentModel.DataAnnotations;

namespace SafeZone.Server.Models;

public class IncidentCategory
{
    [Key]
    public Guid CategoryId { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string? Icon { get; set; }
    
    [MaxLength(20)]
    public string? Color { get; set; }
    
    [MaxLength(200)]
    public string? Description { get; set; }

    public ICollection<Incident> Incidents { get; set; } = new List<Incident>();
}
