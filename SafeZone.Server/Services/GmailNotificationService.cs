using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace SafeZone.Server.Services;

public sealed class GmailNotificationService : IGmailNotificationService
{
    private readonly IConfiguration _configuration;
    private readonly bool _isConfigured;

    public GmailNotificationService(IConfiguration configuration)
    {
        _configuration = configuration;
        _isConfigured = !string.IsNullOrWhiteSpace(configuration["Gmail:ApiKey"]);
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string body)
    {
        if (!_isConfigured)
        {
            Console.WriteLine("[Gmail] API key not configured — skipping email to {0}.", to);
            return false;
        }

        Console.WriteLine("[Gmail] [SKELETON] Sending email to {0}: {1}", to, subject);
        await Task.CompletedTask;
        return true;
    }

    public async Task<bool> SendFirStatusEmailAsync(string to, string firNumber, string status)
    {
        var subject = $"FIR {firNumber} — Status Update";
        var body = $"Your FIR #{firNumber} has been {status.ToLowerInvariant()} by the reviewing authority.";

        return await SendEmailAsync(to, subject, body);
    }

    public async Task<bool> SendIncidentAlertAsync(string to, string incidentTitle, string severity)
    {
        var subject = $"[{severity}] Incident Alert: {incidentTitle}";
        var body = $"A {severity.ToLowerInvariant()} severity incident has been reported: {incidentTitle}";

        return await SendEmailAsync(to, subject, body);
    }
}
