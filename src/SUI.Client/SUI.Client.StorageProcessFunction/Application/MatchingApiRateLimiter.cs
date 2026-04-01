using Microsoft.Extensions.Options;

namespace SUI.StorageProcessFunction.Application;

public sealed class MatchingApiRateLimiter(
    TimeProvider timeProvider,
    IOptions<StorageProcessFunctionOptions> options
) : IMatchingApiRateLimiter
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private DateTimeOffset? _lastRequestStartedAt;

    public async Task WaitAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var maxRequestsPerSecond = options.Value.MaxRequestsPerSecond;
            if (maxRequestsPerSecond <= 0)
            {
                throw new InvalidOperationException(
                    "StorageProcessFunction MaxRequestsPerSecond must be greater than zero."
                );
            }

            var interval = TimeSpan.FromSeconds(1d / maxRequestsPerSecond);
            var now = timeProvider.GetUtcNow();

            if (_lastRequestStartedAt is { } lastRequestStartedAt)
            {
                var elapsed = now - lastRequestStartedAt;
                if (elapsed < interval)
                {
                    await Task.Delay(interval - elapsed, timeProvider, cancellationToken);
                }
            }

            _lastRequestStartedAt = timeProvider.GetUtcNow();
        }
        finally
        {
            _gate.Release();
        }
    }
}
