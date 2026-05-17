using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SafeZone.Server.Models;

public class Notification
{
    [Key]
    public Guid NotificationId { get; set; } = Guid.NewGuid();
    
    public Guid UserId { get; set; }
    
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;
    
    public string Message { get; set; } = string.Empty;
    
    public bool IsRead { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ReadAt { get; set; }
    
    [MaxLength(255)]
    public string? Link { get; set; }
    
    public Guid? RelatedEntityId { get; set; }
    
    [MaxLength(50)]
    public string? RelatedEntityType { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}
