namespace SafeZone.Server.Services;

public interface IVoiceCallService
{
    Task<CallSession> StartOutboundCallAsync(
        string phoneNumber,
        string? systemPrompt = null,
        Guid? triggeredByUserId = null,
        CancellationToken cancellationToken = default);

    Task<CallSession?> GetCallAsync(Guid callId);

    Task<List<CallSession>> GetActiveCallsAsync();

    Task EndCallAsync(Guid callId, string? reason = null);

    Task<string?> GetFullTranscriptAsync(Guid callId);

    bool IsMockMode { get; }
}
