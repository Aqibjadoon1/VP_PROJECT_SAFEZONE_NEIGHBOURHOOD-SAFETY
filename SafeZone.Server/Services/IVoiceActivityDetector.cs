namespace SafeZone.Server.Services;

public interface IVoiceActivityDetector : IDisposable
{
    bool IsSpeech(byte[] audioData, int sampleRate = 16000);

    float LastSpeechProbability { get; }
}
