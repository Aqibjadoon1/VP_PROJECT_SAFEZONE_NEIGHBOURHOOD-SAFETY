namespace SafeZone.Server.Services;

public class MockTtsService : ITextToSpeech
{
    public int SampleRate => 16000;
    public int Channels => 1;
    public bool IsMock => true;

    private static readonly byte[] _silentWavHeader = new byte[]
    {
        0x52, 0x49, 0x46, 0x46,
        0x00, 0x00, 0x00, 0x00,
        0x57, 0x41, 0x56, 0x45,
        0x66, 0x6D, 0x74, 0x20,
        0x10, 0x00, 0x00, 0x00,
        0x01, 0x00,
        0x01, 0x00,
        0x80, 0x3E, 0x00, 0x00,
        0x00, 0x7D, 0x00, 0x00,
        0x02, 0x00,
        0x10, 0x00,
        0x64, 0x61, 0x74, 0x61,
        0x00, 0x00, 0x00, 0x00
    };

    public Task<byte[]> SynthesizeAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Task.FromResult(Array.Empty<byte>());
        }

        var durationMs = Math.Max(500, text.Length * 60);
        var sampleCount = (int)(SampleRate * durationMs / 1000.0);
        var byteCount = sampleCount * 2;

        var wavData = new byte[_silentWavHeader.Length + byteCount];
        _silentWavHeader.CopyTo(wavData, 0);

        var fileSize = wavData.Length - 8;
        wavData[4] = (byte)(fileSize & 0xFF);
        wavData[5] = (byte)((fileSize >> 8) & 0xFF);
        wavData[6] = (byte)((fileSize >> 16) & 0xFF);
        wavData[7] = (byte)((fileSize >> 24) & 0xFF);

        var dataSize = byteCount;
        wavData[40] = (byte)(dataSize & 0xFF);
        wavData[41] = (byte)((dataSize >> 8) & 0xFF);
        wavData[42] = (byte)((dataSize >> 16) & 0xFF);
        wavData[43] = (byte)((dataSize >> 24) & 0xFF);

        var random = new Random(text.GetHashCode());
        for (int i = 0; i < sampleCount; i++)
        {
            var sample = (short)(random.NextDouble() * 50 - 25);
            var offset = _silentWavHeader.Length + i * 2;
            wavData[offset] = (byte)(sample & 0xFF);
            wavData[offset + 1] = (byte)((sample >> 8) & 0xFF);
        }

        return Task.FromResult(wavData);
    }

    public void Dispose()
    {
    }
}
