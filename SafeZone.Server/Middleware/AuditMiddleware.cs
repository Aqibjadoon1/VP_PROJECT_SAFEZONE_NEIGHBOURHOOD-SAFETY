using System.Security.Claims;
using SafeZone.Server.Services;

namespace SafeZone.Server.Middleware;

public sealed class AuditMiddleware
{
    private readonly RequestDelegate _next;

    public AuditMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAuditLogService auditLog)
    {
        var method = context.Request.Method;
        var path = context.Request.Path.ToString();

        if (method is "POST" or "PUT" or "DELETE" or "PATCH")
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ip = context.Connection.RemoteIpAddress?.ToString();

            await auditLog.LogAsync(
                method,
                "API",
                path,
                $"IP: {ip}",
                userId);
        }

        await _next(context);
    }
}
