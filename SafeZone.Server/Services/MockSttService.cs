namespace SafeZone.Server.Services;

public class MockSttService : ISpeechToText
{
    private static readonly string[] _mockTranscripts = new[]
    {
        "Help, there's an emergency!",
        "There's a fire at my location.",
        "Someone is injured, please send an ambulance.",
        "I see suspicious activity, need police.",
        "There's been an accident.",
        "Please help, I'm in danger.",
        "My location is critical.",
        "Emergency services needed immediately."
    };

    private int _transcriptIndex = 0;
    private readonly object _lock = new();

    public bool IsMock => true;

    public Task<string> TranscribeAsync(byte[] audioData, int sampleRate = 16000)
    {
        if (audioData == null || audioData.Length == 0)
        {
            return Task.FromResult(string.Empty);
        }

        lock (_lock)
        {
            var transcript = _mockTranscripts[_transcriptIndex % _mockTranscripts.Length];
            _transcriptIndex++;
            return Task.FromResult(transcript);
        }
    }

    public async IAsyncEnumerable<string> TranscribeStreamAsync(
        Stream audioStream,
        int sampleRate = 16000,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken = default)
    {
        var buffer = new byte[4096];
        int bytesRead;

        while ((bytesRead = await audioStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
        {
            if (bytesRead > 100)
            {
                lock (_lock)
                {
                    var transcript = _mockTranscripts[_transcriptIndex % _mockTranscripts.Length];
                    _transcriptIndex++;
                    yield return transcript;
                }
            }

            await Task.Delay(100, cancellationToken);
        }
    }

    public void Dispose()
    {
    }
}
