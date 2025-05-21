using ExternalApi.Services;
using Shared.Models;
using Task = System.Threading.Tasks.Task;

namespace Unit.Tests.External.NhsFhirClientTests;

public class PerformSearchTests : BaseNhsFhirClientTests
{
    [Fact]
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
        Assert.NotNull(result);
        Assert.Equal(SearchResult.ResultType.Matched, result.Type);
        Assert.Equal("123", result.NhsNumber);
    }

    [Fact]
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
        Assert.NotNull(result);
        Assert.Equal(SearchResult.ResultType.MultiMatched, result.Type);
    }

    [Fact]
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
        Assert.NotNull(result);
        Assert.Equal(SearchResult.ResultType.Unmatched, result.Type);
    }
}