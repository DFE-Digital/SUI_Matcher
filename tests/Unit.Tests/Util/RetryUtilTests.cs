using Microsoft.Extensions.Logging;

using Moq;

using Shared.Util;

namespace Unit.Tests.Util;

public class RetryUtilTests
{
    [Fact]
    public async Task RetryAsync_Should_Succeed_On_First_Attempt()
    {
        var logger = new Mock<ILogger>();
        var action = new Func<Task>(() => Task.CompletedTask);

        await RetryUtil.RetryAsync(action, 3, 100, logger.Object);

        logger.Verify(l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task RetryAsync_Should_Succeed_On_Second_Attempt()
    {
        var logger = new Mock<ILogger>();
        var attempts = 0;

        var action = new Func<Task>(() =>
        {
            attempts++;
            if (attempts < 2)
                throw new Exception("Simulated failure");
            return Task.CompletedTask;
        });

        await RetryUtil.RetryAsync(action, 3, 100, logger.Object);

        logger.Verify(l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(1));
    }

    [Fact]
    public async Task RetryAsync_Should_NotGoBeyondMaxAttempts()
    {
        var logger = new Mock<ILogger>();
        var attempts = 0;

        var action = new Func<Task>(() =>
        {
            attempts++;
            throw new Exception("Simulated failure");
        });

        await Assert.ThrowsAsync<Exception>(() => RetryUtil.RetryAsync(action, 3, 100, logger.Object));

        Assert.Equal(3, attempts);
        logger.Verify(l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(3));
    }
}