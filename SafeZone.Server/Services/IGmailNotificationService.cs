namespace SafeZone.Server.Services;

public interface IGmailNotificationService
{
    Task<bool> SendEmailAsync(string to, string subject, string body);
    Task<bool> SendFirStatusEmailAsync(string to, string firNumber, string status);
    Task<bool> SendIncidentAlertAsync(string to, string incidentTitle, string severity);
}
