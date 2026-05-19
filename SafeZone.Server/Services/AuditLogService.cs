using Microsoft.Extensions.Logging;

namespace SafeZone.Server.Services;

public sealed class AuditLogService : IAuditLogService
{
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(ILogger<AuditLogService> logger)
    {
        _logger = logger;
    }

    public Task LogAsync(string action, string entityType, string? entityId, string? details, string? userId)
    {
        var timestamp = DateTime.UtcNow.ToString("O");
        var userInfo = userId ?? "system";

        _logger.LogInformation(
            "[AUDIT] {Timestamp} | Action: {Action} | Entity: {EntityType}:{EntityId} | User: {UserId} | {Details}",
            timestamp, action, entityType, entityId ?? "N/A", userInfo, details ?? string.Empty);

        return Task.CompletedTask;
    }
}
