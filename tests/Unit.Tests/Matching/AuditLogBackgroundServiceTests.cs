using System.Security.Claims;
using System.Threading.Channels;

using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;

using MatchingApi;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using Shared;
using Shared.Logging;

namespace Unit.Tests.Matching;

public class AuditLogBackgroundServiceTests
{
    private readonly Mock<ILogger<AuditLogBackgroundService>> _logger;
    private readonly Channel<AuditLogEntry> _channel = Channel.CreateUnbounded<AuditLogEntry>();
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor = new();
    private readonly DefaultHttpContext _defaultHttpContext = new();

    public AuditLogBackgroundServiceTests()
    {
        _logger = new Mock<ILogger<AuditLogBackgroundService>>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldWriteAuditLogEntry()
    {
        // Arrange
        _defaultHttpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity([
                new Claim(ClaimTypes.Name, "TestUser")
            ])
        );
        var mockTableClient = new Mock<TableClient>();
        mockTableClient
            .Setup(x => x.AddEntityAsync(It.IsAny<TableEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response>());
        var mockTableServiceClient = new Mock<TableServiceClient>();
        mockTableServiceClient
            .Setup(x => x.CreateTableIfNotExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<TableItem>>());
        mockTableServiceClient.Setup(x => x.GetTableClient(Constants.AuditLog.AzStorageTableName)).Returns(mockTableClient.Object);


        // Use mockTableClient.Object in your service
        var service = new AuditLogBackgroundService(_logger.Object, _channel, mockTableServiceClient.Object, _httpContextAccessor.Object);
        _httpContextAccessor.Setup(x => x.HttpContext).Returns(_defaultHttpContext);
        var entry = new AuditLogEntry(AuditLogEntry.AuditLogAction.Match, new Dictionary<string, string>() { { "SearchId", "12345" } });

        // Act
        await _channel.Writer.WriteAsync(entry);
        await service.StartAsync(CancellationToken.None);

        // Assert
        mockTableClient.Verify(x => x.AddEntityAsync(It.Is<TableEntity>(entity =>
            entity.ContainsKey("UserId")
            && entity["UserId"].ToString() == "TestUser"
            && entity.ContainsKey("Timestamp")
            && entity.ContainsKey("Action")
            && entity["Action"].ToString() == "Match"
            && entity.ContainsKey("SearchId")
            && entity["SearchId"].ToString() == "12345"
            && entity.ContainsKey("Metadata")
            && entity.PartitionKey == $"TestUser_{DateTime.UtcNow:yyyy-MM-dd}"),
            It.IsAny<CancellationToken>()), Times.Once);
        mockTableServiceClient.Verify(x => x.CreateTableIfNotExistsAsync(Constants.AuditLog.AzStorageTableName, It.IsAny<CancellationToken>()), Times.Once);
        _httpContextAccessor.Verify(x => x.HttpContext, Times.Once);
    }

    [Fact]
    public async Task ShouldLogWarningIfUserUnknown()
    {
        // Arrange
        _defaultHttpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity([
            ])
        );
        var mockTableClient = new Mock<TableClient>();
        mockTableClient
            .Setup(x => x.AddEntityAsync(It.IsAny<TableEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response>());
        var mockTableServiceClient = new Mock<TableServiceClient>();
        mockTableServiceClient
            .Setup(x => x.CreateTableIfNotExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<TableItem>>());
        mockTableServiceClient.Setup(x => x.GetTableClient(Constants.AuditLog.AzStorageTableName)).Returns(mockTableClient.Object);


        // Use mockTableClient.Object in your service
        var service = new AuditLogBackgroundService(_logger.Object, _channel, mockTableServiceClient.Object, _httpContextAccessor.Object);
        _httpContextAccessor.Setup(x => x.HttpContext).Returns(_defaultHttpContext);
        var entry = new AuditLogEntry(AuditLogEntry.AuditLogAction.Match, new Dictionary<string, string>() { { "SearchId", "12345" } });

        // Act
        await _channel.Writer.WriteAsync(entry);
        await service.StartAsync(CancellationToken.None);

        // Assert
        _logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Audit log entry created with unknown user.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}