using System.Collections.Concurrent;

namespace SafeZone.Server.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly int _maxRequests;
    private readonly TimeSpan _window;
    private readonly ConcurrentDictionary<string, ClientBucket> _clients = new();

    public RateLimitingMiddleware(RequestDelegate next, int maxRequests = 60, int windowSeconds = 60)
    {
        _next = next;
        _maxRequests = maxRequests;
        _window = TimeSpan.FromSeconds(windowSeconds);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var key = $"{clientIp}:{context.Request.Path}";
        var now = DateTime.UtcNow;

        var bucket = _clients.GetOrAdd(key, _ => new ClientBucket { WindowStart = now, Count = 0 });

        lock (bucket)
        {
            if (now - bucket.WindowStart > _window)
            {
                bucket.WindowStart = now;
                bucket.Count = 0;
            }

            bucket.Count++;

            if (bucket.Count > _maxRequests)
            {
                context.Response.StatusCode = 429;
                context.Response.Headers["Retry-After"] = _window.TotalSeconds.ToString("F0");
                context.Response.ContentType = "application/json";
                context.Response.WriteAsync(
                    $"{{\"error\":\"Too many requests. Limit: {_maxRequests}/{_window.TotalSeconds:F0}s\",\"retryAfter\":{_window.TotalSeconds:F0}}}");
                return;
            }
        }

        await _next(context);
    }

    private class ClientBucket
    {
        public DateTime WindowStart { get; set; }
        public int Count { get; set; }
    }
}
