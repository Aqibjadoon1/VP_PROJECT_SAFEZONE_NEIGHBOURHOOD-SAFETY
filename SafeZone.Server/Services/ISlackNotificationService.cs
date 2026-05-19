namespace SafeZone.Server.Services;

public interface ISlackNotificationService
{
    Task<bool> SendAlertAsync(string title, string message, string severity);
}
