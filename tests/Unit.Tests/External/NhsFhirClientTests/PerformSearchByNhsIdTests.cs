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

    public async Task ShouldReturnDemographicResult_WithGeneralPractitioner_WhenPersonFound()
    {
        // Arrange
        var testFhirClient = new TestFhirClientSuccess("https://fhir.api.endpoint");
        _fhirClientFactory.Setup(f => f.CreateFhirClient())
            .Returns(testFhirClient);
        
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
        _fhirClientFactory.Setup(f => f.CreateFhirClient())
            .Returns(testFhirClient);
        
        var client = new NhsFhirClient(_fhirClientFactory.Object, _loggerMock.Object);
        
        //Act
        var result = await client.PerformSearchByNhsId("1234567890");
        
        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Result);
        Assert.Equal(4, result.Result.FullAddressDetails.Length);
        Assert.Equal(["64 Higher Street Leeds West Yorkshire LS123EA", "54 Medium Street Leeds West Yorkshire LS123EH", "34 Low Street Leeds West Yorkshire LS123EG", "12 High Street Leeds West Yorkshire LS123EF"], result.Result.FullAddressDetails);
    }

    [Fact]
    public async Task ShouldReturnDemographicResultError_WhenFhirClientInvalidNhsNumberErrorOccurs()
    {
        // Arrange
        var testFhirClient = new TestFhirClientError("https://fhir.api.endpoint", "INVALID_NHS_NUMBER");
        _fhirClientFactory.Setup(f => f.CreateFhirClient())
            .Returns(testFhirClient);

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
        var testFhirClient = new TestFhirClientError("https://fhir.api.endpoint", "PATIENT_NOT_FOUND");
        _fhirClientFactory.Setup(f => f.CreateFhirClient())
            .Returns(testFhirClient);

        var client = new NhsFhirClient(_fhirClientFactory.Object, _loggerMock.Object);

        // Act
        var result = await client.PerformSearchByNhsId("1234567890");

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Result);
        Assert.False(string.IsNullOrEmpty(result.ErrorMessage));
    }
}