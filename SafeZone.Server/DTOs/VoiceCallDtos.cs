namespace SafeZone.Server.DTOs;

public record StartCallDto
{
    public string PhoneNumber { get; init; } = string.Empty;
    public string? SystemPrompt { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
}

public record CallResponseDto
{
    public Guid CallId { get; init; }
    public string RemoteNumber { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Direction { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public bool IsMockMode { get; init; }
}

public record TranscriptSegmentDto
{
    public string Speaker { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}
