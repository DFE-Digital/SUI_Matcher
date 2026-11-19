using ExternalApi.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Unit.Tests.External.NhsFhirClientTests;

public class PerformSearchByNhsIdTests : BaseNhsFhirClientTests
{
    [Fact]
    public async Task ShouldReturnDemographicResult_WhenPersonFound()
    {
        // Arrange
        var testFhirClient = new TestFhirClientSuccess("https://fhir.api.endpoint");
        _fhirClientFactory.Setup(f => f.CreateFhirClient()).Returns(testFhirClient);

        var client = new NhsFhirClient(_fhirClientFactory.Object, _loggerMock.Object);

        // Act
        var result = await client.PerformSearchByNhsId("1234567890");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Result);
    }

    [Fact]
    public async Task ShouldReturnDemographicResult_WithGeneralPractitioner_WhenPersonFound()
    {
        // Arrange
        var testFhirClient = new TestFhirClientSuccess("https://fhir.api.endpoint");
        _fhirClientFactory.Setup(f => f.CreateFhirClient()).Returns(testFhirClient);

        var client = new NhsFhirClient(_fhirClientFactory.Object, _loggerMock.Object);

        //Act
        var result = await client.PerformSearchByNhsId("1234567890");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Result);
        Assert.Equal("Y12345", result.Result.GeneralPractitionerOdsId);
    }

    [Fact]
    public async Task ShouldReturnDemographicResult_WithFullAddressDetails_WhenPersonFound()
    {
        // Arrange
        var testFhirClient = new TestFhirClientSuccess("https://fhir.api.endpoint");
        _fhirClientFactory.Setup(f => f.CreateFhirClient()).Returns(testFhirClient);

        var client = new NhsFhirClient(_fhirClientFactory.Object, _loggerMock.Object);

        //Act
        var result = await client.PerformSearchByNhsId("1234567890");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Result);
        Assert.Equal(4, result.Result.AddressHistory.Length);
        Assert.Equal(
            [
                "home~64 Higher Street~Leeds~West Yorkshire~LS123EA|",
                "billing~54 Medium Street~Leeds~West Yorkshire~LS123EH|",
                "work~34 Low Street~Leeds~West Yorkshire~LS123EG|",
                "temp~12 High,Street~Leeds~West Yorkshire~LS123EF|",
            ],
            result.Result.AddressHistory
        );
    }

    [Fact]
    public async Task ShouldReturnDemographicResultError_WhenFhirClientInvalidNhsNumberErrorOccurs()
    {
        // Arrange
        var testFhirClient = new TestFhirClientError(
            "https://fhir.api.endpoint",
            "INVALID_NHS_NUMBER"
        );
        _fhirClientFactory.Setup(f => f.CreateFhirClient()).Returns(testFhirClient);

        var client = new NhsFhirClient(_fhirClientFactory.Object, _loggerMock.Object);

        // Act
        var result = await client.PerformSearchByNhsId("1234567890");

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Result);
        Assert.False(string.IsNullOrEmpty(result.ErrorMessage));
    }

    [Fact]
    public async Task ShouldReturnDemographicResultError_WhenFhirClientPatientNotFoundErrorOccurs()
    {
        // Arrange
        var testFhirClient = new TestFhirClientError(
            "https://fhir.api.endpoint",
            "PATIENT_NOT_FOUND"
        );
        _fhirClientFactory.Setup(f => f.CreateFhirClient()).Returns(testFhirClient);

        var client = new NhsFhirClient(_fhirClientFactory.Object, _loggerMock.Object);

        // Act
        var result = await client.PerformSearchByNhsId("1234567890");

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Result);
        Assert.False(string.IsNullOrEmpty(result.ErrorMessage));
    }

    [Fact]
    public async Task ShouldLogSecurityFlag_WhenPersonFound()
    {
        // Arrange
        var testFhirClient = new TestFhirClientSuccess("https://fhir.api.endpoint");
        _fhirClientFactory.Setup(f => f.CreateFhirClient()).Returns(testFhirClient);

        var client = new NhsFhirClient(_fhirClientFactory.Object, _loggerMock.Object);

        // Act
        await client.PerformSearchByNhsId("9449305552");

        // Assert
        VerifyLog(LogLevel.Information, "[PdsSecurityFlag] R", Times.Once());
    }

    [Fact]
    public async Task ShouldNotLogSuperseded_WhenNhsNumberMatches()
    {
        // Arrange
        var testFhirClient = new TestFhirClientSuccess("https://fhir.api.endpoint");
        _fhirClientFactory.Setup(f => f.CreateFhirClient()).Returns(testFhirClient);

        var client = new NhsFhirClient(_fhirClientFactory.Object, _loggerMock.Object);

        // Act
        await client.PerformSearchByNhsId("9449305552");

        // Assert
        VerifyLog(LogLevel.Information, "[PdsNhsNumberSuperseded]", Times.Never());
    }

    [Fact]
    public async Task ShouldLogSuperseded_WhenNhsNumberDoesNotMatch()
    {
        // Arrange
        var testFhirClient = new TestFhirClientSuccess("https://fhir.api.endpoint");
        _fhirClientFactory.Setup(f => f.CreateFhirClient()).Returns(testFhirClient);

        var client = new NhsFhirClient(_fhirClientFactory.Object, _loggerMock.Object);

        // Act
        await client.PerformSearchByNhsId("9999999993");

        // Assert
        VerifyLog(
            LogLevel.Information,
            "[PdsNhsNumberSuperseded] Number provided does not match number fetched from PDS.",
            Times.Once()
        );
    }

    [Fact]
    public async Task ShouldLogPdsResponseError_WhenFhirClientErrorOccurs()
    {
        // Arrange
        var testFhirClient = new TestFhirClientError(
            "https://fhir.api.endpoint",
            "INVALID_NHS_NUMBER"
        );
        _fhirClientFactory.Setup(f => f.CreateFhirClient()).Returns(testFhirClient);

        var client = new NhsFhirClient(_fhirClientFactory.Object, _loggerMock.Object);

        // Act
        await client.PerformSearchByNhsId("9999999993");

        // Assert
        VerifyLog(
            LogLevel.Error,
            "[PdsResponseError] Error occurred while performing Nhs Digital FHIR API search by NHS ID",
            Times.Once(),
            hasException: true
        );
    }
}
