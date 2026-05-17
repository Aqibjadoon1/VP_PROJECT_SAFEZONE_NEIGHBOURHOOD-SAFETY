using Microsoft.Extensions.Logging;

namespace SafeZone.Server.Services;

public class VoicePipelineService : IVoicePipeline
{
    private readonly ISpeechToText _stt;
    private readonly ILanguageModel _llm;
    private readonly ITextToSpeech _tts;
    private readonly ILogger<VoicePipelineService> _logger;

    public bool IsMock => _stt.IsMock || _llm.IsMock || _tts.IsMock;

    public VoicePipelineService(
        ISpeechToText stt,
        ILanguageModel llm,
        ITextToSpeech tts,
        ILogger<VoicePipelineService> logger)
    {
        _stt = stt;
        _llm = llm;
        _tts = tts;
        _logger = logger;
    }

    public async Task<string> ProcessTurnAsync(
        byte[] userAudio,
        List<ChatMessage> conversationHistory,
        string? systemPrompt = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("ProcessTurnAsync: audioLength={AudioLength}", userAudio.Length);
        
        var transcript = await _stt.TranscribeAsync(userAudio);
        _logger.LogDebug("STT result: {Transcript}", transcript);
        
        if (string.IsNullOrWhiteSpace(transcript))
        {
            return string.Empty;
        }

        var response = await _llm.GenerateResponseAsync(
            transcript,
            conversationHistory,
            systemPrompt,
            cancellationToken);
        
        _logger.LogDebug("LLM response: {Response}", response.Length > 100 ? response[..100] + "..." : response);
        return response;
    }

    public async Task<(string Transcript, byte[]? ResponseAudio)> ProcessTurnWithAudioAsync(
        byte[] userAudio,
        List<ChatMessage> conversationHistory,
        string? systemPrompt = null,
        CancellationToken cancellationToken = default)
    {
        var transcript = await ProcessTurnAsync(userAudio, conversationHistory, systemPrompt, cancellationToken);
        
        if (string.IsNullOrWhiteSpace(transcript))
        {
            return (string.Empty, null);
        }

        var latestResponse = conversationHistory
            .LastOrDefault(m => m.Role == ChatRole.Assistant)?.Content;
        
        if (string.IsNullOrWhiteSpace(latestResponse))
        {
            latestResponse = "Understood. I'm processing your request.";
        }

        var audio = await _tts.SynthesizeAsync(latestResponse, cancellationToken);
        return (transcript, audio);
    }

    public Task<byte[]> SynthesizeTextAsync(string text, CancellationToken cancellationToken = default)
    {
        return _tts.SynthesizeAsync(text, cancellationToken);
    }

    public Task<string> TranscribeAudioAsync(byte[] audio, CancellationToken cancellationToken = default)
    {
        return _stt.TranscribeAsync(audio);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
