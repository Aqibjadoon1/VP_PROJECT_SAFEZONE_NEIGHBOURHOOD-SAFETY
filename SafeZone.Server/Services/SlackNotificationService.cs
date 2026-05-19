using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace SafeZone.Server.Services;

public sealed class SlackNotificationService : ISlackNotificationService
{
    private readonly HttpClient _httpClient;
    private readonly string? _webhookUrl;

    public SlackNotificationService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _webhookUrl = configuration["Slack:WebhookUrl"];
    }

    public async Task<bool> SendAlertAsync(string title, string message, string severity)
    {
        if (string.IsNullOrWhiteSpace(_webhookUrl))
        {
            Console.WriteLine("[Slack] Webhook URL not configured — skipping notification.");
            return false;
        }

        var color = severity switch
        {
            "Critical" => "#FF3366",
            "High" => "#FF9500",
            "Medium" => "#FFB800",
            _ => "#3B82F6"
        };

        var payload = new
        {
            text = $"*SafeZone Alert:* {title}",
            attachments = new[]
            {
                new
                {
                    color,
                    fields = new[]
                    {
                        new { title = "Severity", value = severity, @short = true },
                        new { title = "Timestamp", value = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"), @short = true }
                    },
                    text = message
                }
            }
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync(_webhookUrl, content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Slack] Failed to post to webhook: {ex.Message}");
            return false;
        }
    }
}
