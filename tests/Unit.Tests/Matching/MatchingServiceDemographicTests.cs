using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using Shared.Models;

using SUI.Core.Domain;
using SUI.Core.Endpoints;
using SUI.Core.Services;

namespace Unit.Tests.Matching;

[TestClass]
public class MatchingServiceDemographicTests
{
    private readonly ValidationService _validationService = new();

    [TestMethod]
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
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Result);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    [DataRow("", "NHS number is required")]
    [DataRow("12345", "NHS number must be 10 digits")]
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
        Assert.IsNotNull(result);
        Assert.IsNull(result.Result);
        Assert.IsTrue(result.Errors.Count != 0, message);
    }
}