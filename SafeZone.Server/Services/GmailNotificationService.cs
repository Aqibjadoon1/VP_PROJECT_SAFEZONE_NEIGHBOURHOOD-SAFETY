using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using System.Text.RegularExpressions;

namespace SafeZone.Server.Services;

public sealed partial class GmailNotificationService : IGmailNotificationService
{
    private readonly ILogger<GmailNotificationService> _logger;
    private readonly string? _clientId;
    private readonly string? _clientSecret;
    private readonly string? _refreshToken;
    private readonly string? _fromEmail;
    private readonly string? _applicationName;
    private readonly bool _isConfigured;

    public GmailNotificationService(IConfiguration configuration, ILogger<GmailNotificationService> logger)
    {
        _logger = logger;
        _clientId = configuration["Gmail:ClientId"];
        _clientSecret = configuration["Gmail:ClientSecret"];
        _refreshToken = configuration["Gmail:RefreshToken"];
        _fromEmail = configuration["Gmail:FromEmail"];
        _applicationName = configuration["Gmail:ApplicationName"] ?? "SafeZone";
        _isConfigured = !string.IsNullOrWhiteSpace(_clientId) &&
                        !string.IsNullOrWhiteSpace(_clientSecret) &&
                        !string.IsNullOrWhiteSpace(_refreshToken) &&
                        !string.IsNullOrWhiteSpace(_fromEmail);
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string body)
    {
        if (!_isConfigured)
        {
            _logger.LogInformation("[Gmail API] Not configured — logging: {Subject} to {Recipient}", subject, to);
            return false;
        }

        if (!IsValidEmail(to))
        {
            _logger.LogWarning("[Gmail API] Invalid recipient: {Recipient}", to);
            return false;
        }

        try
        {
            using var service = new GmailService(new BaseClientService.Initializer
            {
                HttpClientInitializer = GetCredential(),
                ApplicationName = _applicationName
            });

            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(new MailboxAddress(_applicationName, _fromEmail));
            mimeMessage.To.Add(new MailboxAddress("", to));
            mimeMessage.Subject = subject;
            mimeMessage.Body = new TextPart("plain") { Text = body };

            using var stream = new MemoryStream();
            await mimeMessage.WriteToAsync(stream);
            var rawMessage = Convert.ToBase64String(stream.ToArray())
                .Replace('+', '-')
                .Replace('/', '_')
                .Replace("=", "");

            var message = new Message { Raw = rawMessage };
            await service.Users.Messages.Send(message, "me").ExecuteAsync();

            _logger.LogInformation("[Gmail API] Email sent to {Recipient}: {Subject}", to, subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Gmail API] Failed to send to {Recipient}", to);
            return false;
        }
    }

    private UserCredential GetCredential()
    {
        var tokenResponse = new TokenResponse { RefreshToken = _refreshToken };
        return new UserCredential(
            new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = _clientId,
                    ClientSecret = _clientSecret
                }
            }),
            "user",
            tokenResponse);
    }

    public async Task<bool> SendFirStatusEmailAsync(string to, string firNumber, string status)
    {
        return await SendEmailAsync(to,
            $"FIR {firNumber} — Status Update",
            $"Your FIR #{firNumber} has been {status.ToLowerInvariant()} by the reviewing authority.\n\nStatus: {status}\nReference: {firNumber}\n\n— SafeZone Emergency System");
    }

    public async Task<bool> SendIncidentAlertAsync(string to, string incidentTitle, string severity)
    {
        return await SendEmailAsync(to,
            $"[{severity}] Incident Alert: {incidentTitle}",
            $"A {severity.ToLowerInvariant()} severity incident has been reported.\n\nTitle: {incidentTitle}\nSeverity: {severity}\n\n— SafeZone Emergency System");
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
    private static bool IsValidEmail(string email) => EmailRegex().IsMatch(email);
}
