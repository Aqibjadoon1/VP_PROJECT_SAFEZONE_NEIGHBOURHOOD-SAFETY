using System.Collections.Concurrent;

namespace SafeZone.Server.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly int _maxRequests;
    private readonly TimeSpan _window;
    private readonly ConcurrentDictionary<string, ClientBucket> _clients = new();
    private readonly TimeSpan _evictionInterval;
    private DateTime _lastEviction = DateTime.UtcNow;

    public RateLimitingMiddleware(RequestDelegate next, int maxRequests = 60, int windowSeconds = 60)
    {
        _next = next;
        _maxRequests = maxRequests;
        _window = TimeSpan.FromSeconds(windowSeconds);
        _evictionInterval = TimeSpan.FromSeconds(windowSeconds * 2);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var key = $"{clientIp}:{context.Request.Path}";
        var now = DateTime.UtcNow;

        // Periodic eviction to prevent unbounded memory growth
        if (now - _lastEviction > _evictionInterval)
        {
            EvictOldBuckets(now);
            _lastEviction = now;
        }

        var bucket = _clients.GetOrAdd(key, _ => new ClientBucket { WindowStart = now, Count = 0 });

        bool isRateLimited;
        lock (bucket)
        {
            if (now - bucket.WindowStart > _window)
            {
                bucket.WindowStart = now;
                bucket.Count = 0;
            }

            bucket.Count++;
            isRateLimited = bucket.Count > _maxRequests;
        }

        if (isRateLimited)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.RetryAfter = _window.TotalSeconds.ToString("F0");
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                $"{{\"error\":\"Too many requests. Limit: {_maxRequests}/{_window.TotalSeconds:F0}s\",\"retryAfter\":{_window.TotalSeconds:F0}}}");
            return;
        }

        await _next(context);
    }

    private void EvictOldBuckets(DateTime now)
    {
        var cutoff = now - _window;
        foreach (var key in _clients.Keys)
        {
            if (_clients.TryGetValue(key, out var bucket))
            {
                lock (bucket)
                {
                    if (bucket.WindowStart < cutoff)
                    {
                        _clients.TryRemove(key, out _);
                    }
                }
            }
        }
    }

    private class ClientBucket
    {
        public DateTime WindowStart { get; set; }
        public int Count { get; set; }
    }
}
