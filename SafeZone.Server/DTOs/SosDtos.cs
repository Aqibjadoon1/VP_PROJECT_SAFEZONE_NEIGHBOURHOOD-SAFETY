using System.ComponentModel.DataAnnotations;
using SafeZone.Server.Models;

namespace SafeZone.Server.DTOs;

public record TriggerSosDto
{
    [Required]
    public AuthorityType EmergencyType { get; init; }
    
    public double Latitude { get; init; }
    
    public double Longitude { get; init; }
    
    [MaxLength(500)]
    public string? AdditionalNotes { get; init; }
    
    public bool NotifyEmergencyContacts { get; init; } = true;
}

public record SosResponseDto
{
    public Guid CallLogId { get; init; }
    
    public string CallType { get; init; } = string.Empty;
    
    public CallStatus Status { get; init; }
    
    public DateTime InitiatedAt { get; init; }
    
    public string? PhoneNumberCalled { get; init; }
    
    public string? AIScript { get; init; }
    
    public bool IsMockMode { get; init; }
    
    public string? Message { get; init; }
}

public record SosCallLogDto
{
    public Guid LogId { get; init; }
    
    public string CallType { get; init; } = string.Empty;
    
    public string? PhoneNumberCalled { get; init; }
    
    public string? CalledNumbers { get; init; }
    
    public CallStatus Status { get; init; }
    
    public DateTime InitiatedAt { get; init; }
    
    public DateTime? CompletedAt { get; init; }
    
    public int DurationSeconds { get; init; }
    
    public bool IsFalseAlarm { get; init; }
    
    public string? AIScript { get; init; }
    
    public double? IncidentLatitude { get; init; }
    
    public double? IncidentLongitude { get; init; }
    
    public string? IncidentTitle { get; init; }
    
    public string? TriggeredByUserName { get; init; }
}
