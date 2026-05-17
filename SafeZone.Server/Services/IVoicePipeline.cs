namespace SafeZone.Server.Services;

public interface IVoicePipeline : IDisposable
{
    Task<string> ProcessTurnAsync(
        byte[] userAudio,
        List<ChatMessage> conversationHistory,
        string? systemPrompt = null,
        CancellationToken cancellationToken = default);

    Task<(string Transcript, byte[]? ResponseAudio)> ProcessTurnWithAudioAsync(
        byte[] userAudio,
        List<ChatMessage> conversationHistory,
        string? systemPrompt = null,
        CancellationToken cancellationToken = default);

    Task<byte[]> SynthesizeTextAsync(string text, CancellationToken cancellationToken = default);

    Task<string> TranscribeAudioAsync(byte[] audio, CancellationToken cancellationToken = default);
}
