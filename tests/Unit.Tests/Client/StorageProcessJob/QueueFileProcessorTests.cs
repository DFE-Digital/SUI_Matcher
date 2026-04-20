using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;
using SUI.Client.StorageProcessJob;
using SUI.Client.StorageProcessJob.Application;
using SUI.Client.StorageProcessJob.Application.Interfaces;
using SUI.Client.StorageProcessJob.Infrastructure;

namespace Unit.Tests.Client.StorageProcessJob;

public class QueueFileProcessorTests
{
    [Fact]
    public async Task Should_ReadQueueMessage_When_RunAsyncStarts()
    {
        var harness = new TestHarness();

        await harness.Sut.RunAsync(CancellationToken.None);

        harness.VerifyQueueMessageWasRead();
    }

    [Fact]
    public async Task Should_DeleteUsingRenewedPopReceipt_When_ProcessingSucceeds()
    {
        var harness = new TestHarness();
        harness.ProcessBlobWhileRenewingVisibility();

        await harness.Sut.RunAsync(CancellationToken.None);

        harness.VerifyDeletedMessage("renewed-pop-receipt");
    }

    [Fact]
    public async Task Should_DeleteMessage_When_RunCancellationIsRequestedAfterProcessingSucceeds()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var harness = new TestHarness(cancellationTokenSource.Token);
        harness.CancelRunTokenAfterProcessingCompletes(cancellationTokenSource);

        await harness.Sut.RunAsync(cancellationTokenSource.Token);

        harness.VerifyDeletedMessage("pop-receipt");
    }

    [Fact]
    public async Task Should_NotDeleteMessage_When_ParsingFails()
    {
        var harness = new TestHarness();
        harness
            .MessageParser.Setup(x => x.Parse(harness.QueueMessage.MessageText))
            .Throws(new InvalidOperationException("Invalid queue message."));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Sut.RunAsync(CancellationToken.None)
        );

        harness.VerifyMessageWasNotDeleted();
    }

    [Fact]
    public async Task Should_NotDeleteMessage_When_BlobProcessingFails()
    {
        var harness = new TestHarness();
        harness
            .BlobFileOrchestrator.Setup(x =>
                x.ProcessAsync(harness.BlobMessage, CancellationToken.None)
            )
            .ThrowsAsync(new InvalidOperationException("Processing failed."));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Sut.RunAsync(CancellationToken.None)
        );

        harness.VerifyMessageWasNotDeleted();
    }

    [Fact]
    public async Task Should_NotDeleteMessage_When_VisibilityRenewalFailsBeforeProcessingCompletes()
    {
        var harness = new TestHarness();
        harness.FailVisibilityRenewalWhileProcessingContinues();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Sut.RunAsync(CancellationToken.None)
        );

        harness.VerifyMessageWasNotDeleted();
    }

    [Fact]
    public async Task Should_DeleteMessage_When_VisibilityRenewalFailsAfterProcessingCompletes()
    {
        var harness = new TestHarness();
        harness.FailVisibilityRenewalAfterProcessingCompletes();

        await harness.Sut.RunAsync(CancellationToken.None);

        harness.VerifyDeletedMessage("pop-receipt");
    }

    [Fact]
    public async Task Should_StartProcessingBeforeDeletingMessage()
    {
        var harness = new TestHarness();
        harness
            .BlobFileOrchestrator.Setup(x =>
                x.ProcessAsync(harness.BlobMessage, CancellationToken.None)
            )
            .Callback(() =>
                Assert.DoesNotContain(
                    harness.QueueClient.Invocations,
                    invocation =>
                        invocation.Method.Name == nameof(IStorageQueueClient.DeleteMessageAsync)
                )
            )
            .Returns(Task.CompletedTask);

        await harness.Sut.RunAsync(CancellationToken.None);

        harness.VerifyDeletedMessage("pop-receipt");
    }

    [Fact]
    public async Task Should_CallBlobOrchestrator_When_QueueMessageIsValid()
    {
        var harness = new TestHarness();

        await harness.Sut.RunAsync(CancellationToken.None);

        harness.VerifyBlobOrchestratorWasCalled();
    }

    [Fact]
    public async Task Should_RenewVisibilityUntil_RunAsyncFinishes()
    {
        var harness = new TestHarness();
        harness.ProcessBlobWhileRenewingVisibility();

        await harness.Sut.RunAsync(CancellationToken.None);

        harness.VerifyVisibilityWasRenewed();
        harness.VerifyVisibilityWasNotRenewedAfterRunCompleted();
    }

    // Help with cutting down the unit test sizes
    // Consider base class if we have more than 1 test file
    private sealed class TestHarness
    {
        public Mock<IStorageQueueClient> QueueClient { get; } = new();
        public Mock<IStorageQueueMessageParser> MessageParser { get; } = new();
        public Mock<IBlobFileOrchestrator> BlobFileOrchestrator { get; } = new();
        private FakeTimeProvider TimeProvider { get; } = new();

        public StorageQueueMessage QueueMessage { get; } =
            new("queue-message-body", "message-id", "pop-receipt");

        public StorageBlobMessage BlobMessage { get; } = new("incoming", "test-file.csv");
        private CancellationToken RunCancellationToken { get; }

        public QueueFileProcessor Sut { get; }

        public TestHarness(CancellationToken runCancellationToken = default)
        {
            RunCancellationToken = runCancellationToken;
            QueueClient
                .Setup(x => x.FetchMessageAsync(RunCancellationToken))
                .ReturnsAsync(QueueMessage);
            MessageParser.Setup(x => x.Parse(QueueMessage.MessageText)).Returns(BlobMessage);
            QueueClient
                .Setup(x =>
                    x.RenewMessageVisibilityAsync(
                        QueueMessage,
                        TimeSpan.FromMinutes(10),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(QueueMessage with { PopReceipt = "renewed-pop-receipt" });

            Sut = new QueueFileProcessor(
                NullLogger<QueueFileProcessor>.Instance,
                TimeProvider,
                QueueClient.Object,
                MessageParser.Object,
                BlobFileOrchestrator.Object,
                Options.Create(
                    new StorageProcessJobOptions
                    {
                        CsvParserName = "TypeOne",
                        MessageVisibilityTimeoutMinutes = 10,
                        MessageVisibilityRenewalIntervalMinutes = 5,
                    }
                )
            );
        }

        public void ProcessBlobWhileRenewingVisibility()
        {
            BlobFileOrchestrator
                .Setup(x => x.ProcessAsync(BlobMessage, RunCancellationToken))
                .Returns(AdvanceToFirstRenewalAsync);
        }

        public void FailVisibilityRenewalWhileProcessingContinues()
        {
            var renewalAttempted = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously
            );

            QueueClient
                .Setup(x =>
                    x.RenewMessageVisibilityAsync(
                        It.IsAny<StorageQueueMessage>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .Returns(async () =>
                {
                    renewalAttempted.SetResult();
                    await Task.Yield();
                    throw new InvalidOperationException("Renewal failed.");
                });

            BlobFileOrchestrator
                .Setup(x => x.ProcessAsync(BlobMessage, RunCancellationToken))
                .Returns(async () =>
                {
                    await AdvanceToFirstRenewalAsync();
                    await renewalAttempted.Task;
                    await Task.Delay(10);
                });
        }

        public void FailVisibilityRenewalAfterProcessingCompletes()
        {
            var processingCompleted = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously
            );
            var renewalStarted = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously
            );

            BlobFileOrchestrator
                .Setup(x => x.ProcessAsync(BlobMessage, RunCancellationToken))
                .Returns(async () =>
                {
                    await AdvanceToFirstRenewalAsync();
                    await renewalStarted.Task;
                    processingCompleted.SetResult();
                });

            QueueClient
                .Setup(x =>
                    x.RenewMessageVisibilityAsync(
                        It.IsAny<StorageQueueMessage>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .Returns(async () =>
                {
                    renewalStarted.SetResult();
                    await processingCompleted.Task;
                    throw new InvalidOperationException("Renewal failed.");
                });
        }

        /// <summary>
        /// Mimics the app shutting down immediately after processing finishes.
        /// Unlikely this will happen but we should test just in case!
        /// </summary>
        /// <param name="cancellationTokenSource"></param>
        public void CancelRunTokenAfterProcessingCompletes(
            CancellationTokenSource cancellationTokenSource
        )
        {
            BlobFileOrchestrator
                .Setup(x => x.ProcessAsync(BlobMessage, RunCancellationToken))
                .Returns(async () =>
                {
                    await Task.Yield();
                    await cancellationTokenSource.CancelAsync();
                });
        }

        public async Task AdvanceToFirstRenewalAsync()
        {
            await Task.Yield();
            TimeProvider.Advance(TimeSpan.FromMinutes(5));

            await WaitUntilAsync(() =>
                QueueClient.Invocations.Any(invocation =>
                    invocation.Method.Name
                    == nameof(IStorageQueueClient.RenewMessageVisibilityAsync)
                )
            );
        }

        public void VerifyDeletedMessage(string popReceipt)
        {
            QueueClient.Verify(
                x =>
                    x.DeleteMessageAsync(
                        It.Is<StorageQueueMessage>(message =>
                            message.MessageId == QueueMessage.MessageId
                            && message.PopReceipt == popReceipt
                        ),
                        CancellationToken.None
                    ),
                Times.Once
            );
        }

        public void VerifyQueueMessageWasRead()
        {
            QueueClient.Verify(x => x.FetchMessageAsync(RunCancellationToken), Times.Once);
        }

        public void VerifyBlobOrchestratorWasCalled()
        {
            BlobFileOrchestrator.Verify(
                x => x.ProcessAsync(BlobMessage, RunCancellationToken),
                Times.Once
            );
        }

        public void VerifyMessageWasNotDeleted()
        {
            QueueClient.Verify(
                x =>
                    x.DeleteMessageAsync(
                        It.IsAny<StorageQueueMessage>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Never
            );
        }

        public void VerifyVisibilityWasRenewed()
        {
            QueueClient.Verify(
                x =>
                    x.RenewMessageVisibilityAsync(
                        QueueMessage,
                        TimeSpan.FromMinutes(10),
                        It.IsAny<CancellationToken>()
                    ),
                Times.AtLeastOnce
            );
        }

        public void VerifyVisibilityWasNotRenewedAfterRunCompleted()
        {
            var renewalCount = QueueClient.Invocations.Count(invocation =>
                invocation.Method.Name == nameof(IStorageQueueClient.RenewMessageVisibilityAsync)
            );

            TimeProvider.Advance(TimeSpan.FromMinutes(5));

            Assert.Equal(
                renewalCount,
                QueueClient.Invocations.Count(invocation =>
                    invocation.Method.Name
                    == nameof(IStorageQueueClient.RenewMessageVisibilityAsync)
                )
            );
        }
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        while (!condition())
        {
            await Task.Delay(10, cts.Token);
        }
    }
}
