using SafeZone.Server.Services;
using Xunit;

namespace SafeZone.Tests.Integration;

public class VoicePipelineTests
{
    [Fact]
    public async Task Pipeline_WithMockServices_Completes()
    {
        var pipeline = new VoicePipelineService(
            new MockSttService(),
            new MockLlmService(),
            new MockTtsService());

        var audio = new byte[1600];
        var response = await pipeline.ProcessTurnAsync(audio, new(), null, CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(response));
    }

    [Fact]
    public async Task Transcription_ReturnsNonEmptyText()
    {
        var stt = new MockSttService();
        var text = await stt.TranscribeAsync(new byte[1600], 16000);
        Assert.False(string.IsNullOrWhiteSpace(text));
    }

    [Fact]
    public async Task Synthesis_ProducesAudio()
    {
        var tts = new MockTtsService();
        var audio = await tts.SynthesizeAsync("Test message");
        Assert.NotNull(audio);
        Assert.True(audio.Length > 0);
    }
}
