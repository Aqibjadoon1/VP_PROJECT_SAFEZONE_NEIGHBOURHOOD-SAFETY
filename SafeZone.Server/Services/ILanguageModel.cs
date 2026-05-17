namespace SafeZone.Server.Services;

public record ChatMessage
{
    public ChatRole Role { get; init; }
    public string Content { get; init; } = string.Empty;

    public ChatMessage() { }

    public ChatMessage(ChatRole role, string content)
    {
        Role = role;
        Content = content;
    }
}

public enum ChatRole
{
    System,
    User,
    Assistant
}

public interface ILanguageModel : IDisposable
{
    Task<string> GenerateResponseAsync(
        string userMessage,
        List<ChatMessage>? conversationHistory = null,
        string? systemPrompt = null,
        CancellationToken cancellationToken = default);

    bool IsMock { get; }
}
