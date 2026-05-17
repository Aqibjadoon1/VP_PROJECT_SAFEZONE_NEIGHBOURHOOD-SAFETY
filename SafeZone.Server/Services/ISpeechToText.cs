namespace SafeZone.Server.Services;

public interface ISpeechToText : IDisposable
{
    Task<string> TranscribeAsync(byte[] audioData, int sampleRate = 16000);

    IAsyncEnumerable<string> TranscribeStreamAsync(
        Stream audioStream,
        int sampleRate = 16000,
        CancellationToken cancellationToken = default);

    bool IsMock { get; }
}
