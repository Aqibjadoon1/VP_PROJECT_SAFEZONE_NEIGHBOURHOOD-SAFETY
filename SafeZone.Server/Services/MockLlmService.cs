namespace SafeZone.Server.Services;

public class MockLlmService : ILanguageModel
{
    private readonly Dictionary<ChatRole, string> _rolePrefixes = new()
    {
        { ChatRole.System, "[System] " },
        { ChatRole.User, "[User] " },
        { ChatRole.Assistant, "[Assistant] " }
    };

    private readonly string _emergencyResponse =
        "This is the SafeZone AI emergency assistant. I understand there's an emergency situation. " +
        "Please stay on the line. Emergency services have been notified of your location. " +
        "Can you tell me how many people are involved? Are there any immediate hazards I should know about?";

    private readonly string _defaultResponse =
        "Thank you for your message. This is a mock AI response. " +
        "In a real implementation, this would connect to an LLM provider like Groq or OpenAI.";

    public bool IsMock => true;

    public Task<string> GenerateResponseAsync(
        string userMessage,
        List<ChatMessage>? conversationHistory = null,
        string? systemPrompt = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            return Task.FromResult(_defaultResponse);
        }

        var lowerMessage = userMessage.ToLowerInvariant();

        if (lowerMessage.Contains("emergency") ||
            lowerMessage.Contains("help") ||
            lowerMessage.Contains("sos") ||
            lowerMessage.Contains("fire") ||
            lowerMessage.Contains("police") ||
            lowerMessage.Contains("ambulance") ||
            lowerMessage.Contains("accident") ||
            lowerMessage.Contains("hurt") ||
            lowerMessage.Contains("injured"))
        {
            return Task.FromResult(_emergencyResponse);
        }

        if (lowerMessage.Contains("hello") || lowerMessage.Contains("hi"))
        {
            return Task.FromResult("Hello! This is the SafeZone AI assistant. How can I help you today?");
        }

        if (lowerMessage.Contains("location"))
        {
            return Task.FromResult("I understand you're providing location information. I've noted your coordinates. Emergency services are being dispatched to your location. Please remain calm and stay on the line if possible.");
        }

        if (lowerMessage.Contains("yes") || lowerMessage.Contains("ok") || lowerMessage.Contains("okay"))
        {
            return Task.FromResult("Understood. Is there anything else you need to report? Any additional details would help emergency responders.");
        }

        if (lowerMessage.Contains("no"))
        {
            return Task.FromResult("Alright. Please stay safe. Emergency services are on their way. If the situation changes, please let me know immediately.");
        }

        return Task.FromResult(_defaultResponse);
    }

    public void Dispose()
    {
    }
}
