using ExternalApi.Services;

using Shared.Models;

namespace Unit.Tests.External.NhsFhirClientTests;

[TestClass]
public class PerformSearchByNhsIdTests : BaseNhsFhirClientTests
{
    
    [TestMethod]
    public async Task ShouldReturnDemographicResult_WhenPersonFound()
    {
        // Arrange
        var expectedDemographicResult = new DemographicResult { Result = "{id:123}", ErrorMessage = null };
        var testFhirClient = new TestFhirClientSuccess("https://fhir.api.endpoint");
        FhirClientFactory.Setup(f => f.CreateFhirClient())
            .Returns(testFhirClient);
        
        var client = new NhsFhirClient(FhirClientFactory.Object, LoggerMock.Object);
        
        // Act
        var result = await client.PerformSearchByNhsId("1234567890");
        
        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Result);
    }

    [TestMethod]
    public async Task ShouldReturnDemographicResultError_WhenFhirClientErrorOccurs()
    {
        // Arrange
        var expectedDemographicResult = new DemographicResult { Result = "{id:123}", ErrorMessage = null };
        var testFhirClient = new TestFhirClientError("https://fhir.api.endpoint");
        FhirClientFactory.Setup(f => f.CreateFhirClient())
            .Returns(testFhirClient);
        
        var client = new NhsFhirClient(FhirClientFactory.Object, LoggerMock.Object);
        
        // Act
        var result = await client.PerformSearchByNhsId("1234567890");
        
        // Assert
        Assert.IsNotNull(result);
        Assert.IsNull(result.Result);
        Assert.IsTrue(string.IsNullOrEmpty(result.ErrorMessage) == false);
    }
    
}
//
// public virtual Task<TResource?> ReadAsync<TResource>(
//         Uri location, 
//         string? ifNoneMatch = null, 
//         DateTimeOffset? ifModifiedSince = null, 
//         CancellationToken? ct = null)
//     in class Hl7.Fhir.Rest.BaseFhirClient