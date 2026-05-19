namespace SafeZone.Server.Services;

public interface IAuditLogService
{
    Task LogAsync(string action, string entityType, string? entityId, string? details, string? userId);
}
