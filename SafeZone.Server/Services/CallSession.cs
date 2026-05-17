using SafeZone.Server.Models;

namespace SafeZone.Server.Services;

public record CallSession
{
    public Guid CallId { get; init; }
    public string RemoteNumber { get; init; } = string.Empty;
    public CallDirection Direction { get; init; }
    public CallStatus Status { get; set; }
    
    public List<ChatMessage> ConversationHistory { get; init; } = new();
    public List<TranscriptSegment> Transcript { get; init; } = new();
    
    public DateTime CreatedAt { get; init; }
    public DateTime? ConnectedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    
    public string? SystemPrompt { get; init; }
    public Guid? TriggeredByUserId { get; init; }
    public Guid? IncidentId { get; set; }
    public bool IsMock { get; init; }
}

public enum CallDirection
{
    Outbound,
    Inbound
}

public record TranscriptSegment
{
    public SpeakerRole Speaker { get; init; }
    public string Text { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}

public enum SpeakerRole
{
    User,
    Agent
}
