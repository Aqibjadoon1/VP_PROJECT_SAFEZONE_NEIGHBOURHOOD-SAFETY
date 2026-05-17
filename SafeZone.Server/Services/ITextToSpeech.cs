namespace SafeZone.Server.Services;

public interface ITextToSpeech : IDisposable
{
    Task<byte[]> SynthesizeAsync(string text, CancellationToken cancellationToken = default);

    int SampleRate { get; }
    int Channels { get; }
    bool IsMock { get; }
}
