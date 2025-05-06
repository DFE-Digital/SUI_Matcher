using System.Net.Http.Json;

using Shared.Models;

namespace ExternalApi.IntegrationTests;

public class ExternalApiIntegrationTests : IClassFixture<ExternalApiFixture>
{
    private readonly HttpClient _httpClient;

    public ExternalApiIntegrationTests(ExternalApiFixture fixture)
    {
        _httpClient = fixture.CreateDefaultClient();
    }

    [Fact]
    public async Task Search_RetrieveOnlyOneMatchingPatient()
    {
        // Arrange
        var httpClient = _httpClient;

        var query = new SearchQuery()
        {
            FuzzyMatch = true,
            Family = "CHISLETT",
            Given = ["OCTAVIA"],
            Birthdate = ["eq2008-09-20"]
        };

        // Act
        var response = await httpClient.PostAsJsonAsync("/api/v1/search", query);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<SearchResult>();

        // Assert
        Assert.Equal(SearchResult.ResultType.Matched, result!.Type);
        Assert.NotNull(result.NhsNumber);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task Search_RetrieveNoMatchingPatient()
    {
        // Arrange
        var httpClient = _httpClient;

        var query = new SearchQuery()
        {
            FuzzyMatch = true,
            Family = "CHISLETTE",
            Given = ["OCTAVIAN"],
            Birthdate = ["eq2008-09-21"]
        };

        // Act
        var response = await httpClient.PostAsJsonAsync("/api/v1/search", query);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<SearchResult>();

        // Assert
        Assert.Equal(SearchResult.ResultType.Unmatched, result!.Type);
        Assert.Null(result.NhsNumber);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task Search_RetrieveMultipleMatchingPatients()
    {
        // Arrange
        var httpClient = _httpClient;

        var query = new SearchQuery()
        {
            FuzzyMatch = true,
            Family = "CHISLETT",
            Given = ["OCTAVIA"],
            Birthdate = ["ge2008-09-21"]
        };

        // Act
        var response = await httpClient.PostAsJsonAsync("/api/v1/search", query);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<SearchResult>();

        // Assert
        Assert.Equal(SearchResult.ResultType.MultiMatched, result!.Type);
        Assert.Null(result.NhsNumber);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task Demographics_RetrievePatientsDemographics()
    {
        // Arrange
        const string validNhsNumber = "9000000009";

        // Act
        var response = await _httpClient.GetAsync($"/api/v1/demographics/{validNhsNumber}");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<DemographicResponse>();

        // Assert
        Assert.NotNull(result?.Result);
    }

    [Fact]
    public async Task Demographics_ReturnsErrors_WhenNhsNumberIsInvalid()
    {
        // Arrange
        const string invalidNhsNumber = "9000000012";

        // Act
        var response = await _httpClient.GetAsync($"/api/v1/demographics/{invalidNhsNumber}");
        response.EnsureSuccessStatusCode();

        // Assert
        var result = await response.Content.ReadFromJsonAsync<DemographicResponse>();
        Assert.NotNull(result);
        Assert.Null(result.Result);
        Assert.NotNull(result.Errors);
    }
}