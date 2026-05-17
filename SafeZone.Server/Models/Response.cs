using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SafeZone.Server.Models;

public class Response
{
    [Key]
    public Guid ResponseId { get; set; } = Guid.NewGuid();
    
    public Guid IncidentId { get; set; }
    
    public Guid AuthorityId { get; set; }
    
    public string? Notes { get; set; }
    
    public DateTime RespondedAt { get; set; } = DateTime.UtcNow;
    
    [MaxLength(50)]
    public string? StatusUpdate { get; set; }

    [ForeignKey(nameof(IncidentId))]
    public Incident Incident { get; set; } = null!;
    
    [ForeignKey(nameof(AuthorityId))]
    public Authority Authority { get; set; } = null!;
}
