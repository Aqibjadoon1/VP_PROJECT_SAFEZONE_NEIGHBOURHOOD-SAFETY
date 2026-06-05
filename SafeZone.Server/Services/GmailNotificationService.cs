using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SafeZone.Server.Services;

public sealed partial class GmailNotificationService : IGmailNotificationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GmailNotificationService> _logger;
    private readonly string? _smtpHost;
    private readonly int _smtpPort;
    private readonly string? _fromEmail;
    private readonly string? _smtpUser;
    private readonly string? _smtpPass;
    private readonly bool _isConfigured;

    public GmailNotificationService(IConfiguration configuration, ILogger<GmailNotificationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _smtpHost = configuration["Smtp:Host"];
        _smtpPort = configuration.GetValue("Smtp:Port", 587);
        _fromEmail = configuration["Smtp:FromEmail"];
        _smtpUser = configuration["Smtp:User"];
        _smtpPass = configuration["Smtp:Password"];
        _isConfigured = !string.IsNullOrWhiteSpace(_smtpHost) && !string.IsNullOrWhiteSpace(_fromEmail);
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string body)
    {
        if (!_isConfigured)
        {
            _logger.LogInformation("[Gmail] SMTP not configured — logging email to {Recipient}: {Subject}", to, subject);
            return false;
        }

        if (!IsValidEmail(to))
        {
            _logger.LogWarning("[Gmail] Invalid recipient email: {Recipient}", to);
            return false;
        }

        try
        {
            using var client = new SmtpClient(_smtpHost, _smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(_smtpUser, _smtpPass)
            };

            var message = new MailMessage
            {
                From = new MailAddress(_fromEmail!, "SafeZone Alerts"),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };
            message.To.Add(to);

            await client.SendMailAsync(message);
            _logger.LogInformation("[Gmail] Email sent to {Recipient}: {Subject}", to, subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Gmail] Failed to send email to {Recipient}", to);
            return false;
        }
    }

    public async Task<bool> SendFirStatusEmailAsync(string to, string firNumber, string status)
    {
        return await SendEmailAsync(to,
            $"FIR {firNumber} — Status Update",
            $"Your FIR #{firNumber} has been {status.ToLowerInvariant()} by the reviewing authority.\n\nStatus: {status}\nReference: {firNumber}");
    }

    public async Task<bool> SendIncidentAlertAsync(string to, string incidentTitle, string severity)
    {
        return await SendEmailAsync(to,
            $"[{severity}] Incident Alert: {incidentTitle}",
            $"A {severity.ToLowerInvariant()} severity incident has been reported.\n\nTitle: {incidentTitle}\nSeverity: {severity}\nReported via SafeZone Emergency System.");
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
    private static bool IsValidEmail(string email) => EmailRegex().IsMatch(email);
}
