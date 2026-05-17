using Microsoft.Extensions.Logging;

namespace SafeZone.Server.Services;

public class MockSmsService : ISmsService
{
    private readonly ILogger<MockSmsService> _logger;

    public bool IsMockMode => true;

    public MockSmsService(ILogger<MockSmsService> logger)
    {
        _logger = logger;
    }

    public Task<SmsResult> SendSmsAsync(string toNumber, string message)
    {
        var messageId = $"SMS-{Guid.NewGuid():N}";

        _logger.LogInformation(
            "MOCK SMS sent to {Number}: MessageId={MessageId}, Preview={Preview}",
            toNumber,
            messageId,
            message.Length > 50 ? message[..50] + "..." : message);

        return Task.FromResult(new SmsResult
        {
            Success = true,
            MessageId = messageId,
            ToNumber = toNumber,
            IsMock = true,
            SentAt = DateTime.UtcNow
        });
    }

    public async Task<List<SmsResult>> SendBulkSmsAsync(List<string> toNumbers, string message)
    {
        var results = new List<SmsResult>();

        foreach (var number in toNumbers)
        {
            var result = await SendSmsAsync(number, message);
            results.Add(result);
            await Task.Delay(100);
        }

        return results;
    }
}
