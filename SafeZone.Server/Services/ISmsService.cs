namespace SafeZone.Server.Services;

public interface ISmsService
{
    Task<SmsResult> SendSmsAsync(string toNumber, string message);

    Task<List<SmsResult>> SendBulkSmsAsync(List<string> toNumbers, string message);

    bool IsMockMode { get; }
}

public record SmsResult
{
    public bool Success { get; init; }
    public string? MessageId { get; init; }
    public string? ToNumber { get; init; }
    public string? ErrorMessage { get; init; }
    public bool IsMock { get; init; }
    public DateTime SentAt { get; init; }
}
