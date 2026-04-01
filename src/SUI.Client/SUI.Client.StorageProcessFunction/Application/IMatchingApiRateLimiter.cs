namespace SUI.StorageProcessFunction.Application;

public interface IMatchingApiRateLimiter
{
    Task WaitAsync(CancellationToken cancellationToken);
}
