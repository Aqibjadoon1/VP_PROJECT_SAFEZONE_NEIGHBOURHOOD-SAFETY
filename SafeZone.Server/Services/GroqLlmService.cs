using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SafeZone.Server.Helpers;

namespace SafeZone.Server.Services;

 public class GroqLlmService : ILanguageModel
{
    private readonly HttpClient _httpClient;
    private readonly string _modelName;
    private readonly string? _apiKey;
    private readonly string _endpoint;
    private readonly ILogger<GroqLlmService>? _logger;
    private readonly bool _isConfigured;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    public bool IsMock => !_isConfigured;

    public GroqLlmService(
        string? apiKey = null,
        string modelName = "llama-3.1-8b-instant",
        string endpoint = "https://api.groq.com/openai/v1",
        ILogger<GroqLlmService>? logger = null,
        HttpClient? httpClient = null)
    {
         _apiKey = apiKey ?? Environment.GetEnvironmentVariable("GROQ_API_KEY");
        _modelName = modelName;
        _endpoint = endpoint.TrimEnd('/');
        _logger = logger;
        _httpClient = httpClient ?? new HttpClient();

        if (!string.IsNullOrEmpty(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            _isConfigured = true;
            _logger?.LogInformation("GroqLlmService configured with model: {Model}", modelName);
        }
        else
        {
            _logger?.LogWarning("Groq API key not configured, using mock mode");
            _isConfigured = false;
        }
    }

    public async Task<string> GenerateResponseAsync(
        string userMessage,
        List<ChatMessage>? conversationHistory = null,
        string? systemPrompt = null,
        CancellationToken cancellationToken = default)
    {
        if (!_isConfigured)
        {
            _logger?.LogDebug("Groq not configured, using fallback mock response");
            return GetFallbackResponse(userMessage);
        }

        try
        {
            var messages = new List<object>();

            if (!string.IsNullOrWhiteSpace(systemPrompt))
            {
                messages.Add(new { role = "system", content = systemPrompt });
            }

            if (conversationHistory != null)
            {
                foreach (var msg in conversationHistory)
                {
                    messages.Add(new
                    {
                        role = msg.Role switch
                        {
                            ChatRole.System => "system",
                            ChatRole.User => "user",
                            ChatRole.Assistant => "assistant",
                            _ => "user"
                        },
                        content = msg.Content
                    });
                }
            }

            var hasUserMessage = conversationHistory?
                .Any(m => m.Role == ChatRole.User && m.Content == userMessage) ?? false;

            if (!hasUserMessage)
            {
                messages.Add(new { role = "user", content = userMessage });
            }

            var requestBody = new
            {
                model = _modelName,
                messages = messages,
                max_tokens = 1024,
                temperature = 0.3
            };

            var json = JsonSerializer.Serialize(requestBody, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger?.LogDebug("Calling Groq API with {MessageCount} messages", messages.Count);

            var response = await RetryHelper.WithRetryAsync(async ct =>
            {
                var resp = await _httpClient.PostAsync(
                    $"{_endpoint}/chat/completions",
                    content,
                    ct);
                resp.EnsureSuccessStatusCode();
                return resp;
            }, 3, 500, cancellationToken);

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<GroqChatResponse>(responseJson, JsonOptions);

            var responseText = result?.Choices?.FirstOrDefault()?.Message?.Content;
            _logger?.LogDebug("Groq response received: {Length} chars", responseText?.Length ?? 0);

            return responseText ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Groq API call failed, using fallback");
            return GetFallbackResponse(userMessage);
        }
    }

    private string GetFallbackResponse(string userMessage)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            return "Understood. How can I help you today?";
        }

        var lower = userMessage.ToLowerInvariant();

        if (lower.Contains("emergency") || lower.Contains("help") || lower.Contains("sos"))
        {
            return "This is the SafeZone AI emergency assistant. I understand there's an emergency situation. " +
                   "Please stay calm. Can you tell me how many people are involved? " +
                   "Are there any immediate hazards I should know about? " +
                   "Emergency services are being notified of your location.";
        }

        if (lower.Contains("fire"))
        {
            return "I understand there's a fire. Please evacuate immediately if safe to do so. " +
                   "Fire brigade is being dispatched. Stay low to avoid smoke. " +
                   "Can you tell me if anyone is trapped or injured?";
        }

        if (lower.Contains("medical") || lower.Contains("hurt") || lower.Contains("injured") || lower.Contains("ambulance"))
        {
            return "Medical emergency understood. Ambulance is on the way. " +
                   "Can you describe the injuries? Is the person breathing? " +
                   "Please provide first aid if you are trained to do so.";
        }

        if (lower.Contains("police") || lower.Contains("danger") || lower.Contains("threat"))
        {
            return "Police emergency reported. Please ensure your safety first. " +
                   "If you are in immediate danger, find a safe location. " +
                   "Can you describe the situation? Are there any weapons involved?";
        }

        if (lower.Contains("location"))
        {
            return "Your location has been noted and shared with emergency responders. " +
                   "They are navigating to your coordinates. Please stay visible if possible. " +
                   "Is there anything else responders should know about the location?";
        }

        if (lower.Contains("yes"))
        {
            return "Understood. Please provide more details if you can. " +
                   "Every piece of information helps emergency responders prepare better.";
        }

        if (lower.Contains("no"))
        {
            return "Alright. Please remain calm and stay on the line if possible. " +
                   "Emergency services are on their way. Let me know immediately if the situation changes.";
        }

        return "Thank you for the information. I'm processing this and coordinating with emergency services. " +
               "Please stay on the line. Is there anything else I should know?";
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }
}

internal class GroqChatResponse
{
    public List<GroqChoice>? Choices { get; set; }
}

internal class GroqChoice
{
    public GroqMessage? Message { get; set; }
}

internal class GroqMessage
{
    public string? Content { get; set; }
}
