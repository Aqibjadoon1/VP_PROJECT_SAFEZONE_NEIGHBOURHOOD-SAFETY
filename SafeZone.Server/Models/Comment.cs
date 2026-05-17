using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SafeZone.Server.Models;

public class Comment
{
    [Key]
    public Guid CommentId { get; set; } = Guid.NewGuid();
    
    public Guid IncidentId { get; set; }
    
    public Guid UserId { get; set; }
    
    public string Message { get; set; } = string.Empty;
    
    public bool IsOfficialUpdate { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(IncidentId))]
    public Incident Incident { get; set; } = null!;
    
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}
