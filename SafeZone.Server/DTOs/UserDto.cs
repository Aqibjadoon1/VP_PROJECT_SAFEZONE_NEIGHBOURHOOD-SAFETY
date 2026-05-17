using SafeZone.Server.Models;

namespace SafeZone.Server.DTOs;

public class UserDto
{
    public Guid Id { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public double? LastKnownLatitude { get; set; }
    public double? LastKnownLongitude { get; set; }
    public double ProximityRadiusKm { get; set; }
    public bool IsAnonymous { get; set; }
    public bool PushNotificationsEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastActiveAt { get; set; }
    public bool IsActive { get; set; }
}
