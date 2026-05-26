namespace SafeZone.Server.Helpers;

public static class RetryHelper
{
    public static async Task<T> WithRetryAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        int maxAttempts = 3,
        double baseDelayMs = 500,
        CancellationToken cancellationToken = default)
    {
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try { return await operation(cancellationToken); }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                var delay = baseDelayMs * Math.Pow(2, attempt - 1);
                var jitter = Random.Shared.NextDouble() * delay * 0.3;
                await Task.Delay(TimeSpan.FromMilliseconds(delay + jitter), cancellationToken);
            }
        }
        return await operation(cancellationToken);
    }
}
