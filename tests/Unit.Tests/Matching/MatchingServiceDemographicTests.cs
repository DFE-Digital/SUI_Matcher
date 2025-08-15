using MatchingApi.Services;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using Shared.Endpoint;
using Shared.Logging;
using Shared.Models;

namespace Unit.Tests.Matching;

public class MatchingServiceDemographicTests
{
    private readonly ValidationService _validationService = new();
    private readonly Mock<INhsFhirClient> _nhsFhirClient = new(MockBehavior.Loose);
    private readonly Mock<IAuditLogger> _auditLogger = new();

    [Fact]
    public async Task ShouldReturnDemographics()
    {
        // Arrange
        _nhsFhirClient.Setup(x => x.PerformSearchByNhsId("1234567890"))
            .ReturnsAsync(new DemographicResult() { Result = new NhsPerson { NhsNumber = "1234567890" } });
        var sut = new MatchingService(NullLogger<MatchingService>.Instance, _nhsFhirClient.Object, _validationService, _auditLogger.Object);

        var request = new DemographicRequest
        {
            NhsNumber = "1234567890"
        };

        // Act
        var result = await sut.GetDemographicsAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Result);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("", "NHS number is required")]
    [InlineData("12345", "NHS number must be 10 digits")]
    public async Task ShouldReturnErrors_WhenValidationErrorOccurs(string nhsId, string message)
    {
        // Arrange
        var sut = new MatchingService(NullLogger<MatchingService>.Instance, _nhsFhirClient.Object, _validationService, _auditLogger.Object);

        var request = new DemographicRequest
        {
            NhsNumber = nhsId
        };

        // Act
        var result = await sut.GetDemographicsAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Result);
        Assert.True(result.Errors.Count != 0, message);
    }
}