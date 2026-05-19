namespace SafeZone.Server.Services;

public sealed class EnergyVadService : IVoiceActivityDetector
{
    private readonly float _energyThreshold;
    private readonly float _smoothingAlpha;
    private float _smoothedEnergy;
    private bool _disposed;

    public float LastSpeechProbability { get; private set; }

    public EnergyVadService(float energyThreshold = 0.02f, float smoothingAlpha = 0.85f)
    {
        _energyThreshold = energyThreshold;
        _smoothingAlpha = smoothingAlpha;
    }

    public bool IsSpeech(byte[] audioData, int sampleRate = 16000)
    {
        if (_disposed) return false;
        if (audioData is null or { Length: 0 }) return false;

        var rms = CalculateRms(audioData);
        var db = RmsToDecibels(rms);

        _smoothedEnergy = _smoothingAlpha * _smoothedEnergy + (1f - _smoothingAlpha) * db;

        var normalizedDb = Math.Clamp((_smoothedEnergy + 60f) / 60f, 0f, 1f);
        LastSpeechProbability = normalizedDb;

        return normalizedDb > _energyThreshold;
    }

    private static float CalculateRms(byte[] audioData)
    {
        var sampleCount = audioData.Length / 2;
        if (sampleCount == 0) return 0f;

        float sum = 0;
        for (int i = 0; i < audioData.Length - 1; i += 2)
        {
            var sample = BitConverter.ToInt16(audioData, i);
            sum += sample * sample;
        }

        return MathF.Sqrt(sum / sampleCount);
    }

    private static float RmsToDecibels(float rms)
    {
        return rms > 0 ? 20f * MathF.Log10(rms / 32768f) : -100f;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _smoothedEnergy = 0;
        LastSpeechProbability = 0;
    }
}
