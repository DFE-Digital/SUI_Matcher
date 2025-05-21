using MatchingApi.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shared.Endpoint;
using Shared.Models;

namespace Unit.Tests.Matching;

public class MatchingServiceDemographicTests
{
    private readonly ValidationService _validationService = new();

    [Fact]
    public async Task ShouldReturnDemographics()
    {
        // Arrange
        var nhsFhir = new Mock<INhsFhirClient>(MockBehavior.Loose);
        nhsFhir.Setup(x => x.PerformSearchByNhsId("1234567890"))
            .ReturnsAsync(new DemographicResult() { Result = new { Id = "1234567890" } });
        var sut = new MatchingService(NullLogger<MatchingService>.Instance, nhsFhir.Object, _validationService);

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
        var nhsFhir = new Mock<INhsFhirClient>(MockBehavior.Loose);
        var sut = new MatchingService(NullLogger<MatchingService>.Instance, nhsFhir.Object, _validationService);

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