using ExternalApi.Services;

namespace Unit.Tests.External.NhsFhirClientTests;

public class PerformSearchByNhsIdTests : BaseNhsFhirClientTests
{
    [Fact]
    public async Task ShouldReturnDemographicResult_WhenPersonFound()
    {
        // Arrange
        var testFhirClient = new TestFhirClientSuccess("https://fhir.api.endpoint");
        _fhirClientFactory.Setup(f => f.CreateFhirClient())
            .Returns(testFhirClient);

        var client = new NhsFhirClient(_fhirClientFactory.Object, _loggerMock.Object);

        // Act
        var result = await client.PerformSearchByNhsId("1234567890");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Result);
    }

    [Fact]
    public async Task ShouldReturnDemographicResultError_WhenFhirClientErrorOccurs()
    {
        // Arrange
        var testFhirClient = new TestFhirClientError("https://fhir.api.endpoint");
        _fhirClientFactory.Setup(f => f.CreateFhirClient())
            .Returns(testFhirClient);

        var client = new NhsFhirClient(_fhirClientFactory.Object, _loggerMock.Object);

        // Act
        var result = await client.PerformSearchByNhsId("1234567890");

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Result);
        Assert.True(string.IsNullOrEmpty(result.ErrorMessage) == false);
    }

}