using ExternalApi.Services;

using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Moq;

using Shared.Models;

using Task = System.Threading.Tasks.Task;

namespace Unit.Tests.External.NhsFhirClientTests;

[TestClass]
public class PerformSearchTests : BaseNhsFhirClientTests
{

    [TestMethod]
    public async Task ShouldGetSearchResultsMatched_WhenMatched()
    {
        // Arrange
        var searchQuery = new SearchQuery
        {
            Family = "Smith",
            Given = ["John"],
            Gender = "male",
            Birthdate = ["eq1980-01-01"],
        };
        var testFhirClient = new TestFhirClientSuccess("https://fhir.api.endpoint");
        FhirClientFactory.Setup(f => f.CreateFhirClient())
            .Returns(testFhirClient);

        var client = new NhsFhirClient(FhirClientFactory.Object, LoggerMock.Object);

        // Act
        var result = await client.PerformSearch(searchQuery);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(SearchResult.ResultType.Matched, result.Type);
        Assert.AreEqual("123", result.NhsNumber);
    }

    [TestMethod]
    public async Task ShouldGetSearchResultsMultiMatched_WhenMultipleMatches()
    {
        // Arrange
        var searchQuery = new SearchQuery
        {
            Family = "Doe",
            Given = ["John"],
            Birthdate = ["eq1980-01-01"],
        };

        var testFhirClient = new TestFhirClientMultiMatch("https://fhir.api.endpoint");
        FhirClientFactory.Setup(f => f.CreateFhirClient())
            .Returns(testFhirClient);

        var client = new NhsFhirClient(FhirClientFactory.Object, LoggerMock.Object);

        // Act
        var result = await client.PerformSearch(searchQuery);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(SearchResult.ResultType.MultiMatched, result.Type);
    }

    [TestMethod]
    public async Task ShouldGetSearchResultsUnmatched_WhenNoMatches()
    {
        // Arrange
        var searchQuery = new SearchQuery
        {
            Family = "NotExistent",
            Given = ["IAm"],
            Birthdate = ["eq1900-01-01"],
        };

        var testFhirClient = new TestFhirClientUnmatched("https://fhir.api.endpoint");
        FhirClientFactory.Setup(f => f.CreateFhirClient())
            .Returns(testFhirClient);

        var client = new NhsFhirClient(FhirClientFactory.Object, LoggerMock.Object);

        // Act
        var result = await client.PerformSearch(searchQuery);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(SearchResult.ResultType.Unmatched, result.Type);
    }
}