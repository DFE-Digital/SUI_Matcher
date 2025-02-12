using System.Net.Http.Json;
using System.Text.Json;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Shared.Models;
using Xunit.Abstractions;

namespace sui_tests.Tests;

public class ExternalApiIntegrationTests(ITestOutputHelper output)
{
    [Fact]
    public async Task Search_RetrieveOnlyOneMatchingPatient()
    {
        // Arrange
        var app = await StartExternalApi();
        
        var httpClient = app.CreateHttpClient("external-api");
        
        var query = new SearchQuery()
        {
            FuzzyMatch = true,
            Family = "CHISLETT",
            Given = ["OCTAVIA"],
            //Gender = SearchQuery.GenderType.female,
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
        var app = await StartExternalApi();
        
        var httpClient = app.CreateHttpClient("external-api");
        
        var query = new SearchQuery()
        {
            FuzzyMatch = true,
            Family = "CHISLETTE",
            Given = ["OCTAVIAN"],
            //Gender = SearchQuery.GenderType.female,
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
        var app = await StartExternalApi();
        
        var httpClient = app.CreateHttpClient("external-api");
        
        var query = new SearchQuery()
        {
            FuzzyMatch = true,
            Family = "HEAPLEE",
            Gender = SearchQuery.GenderType.male.ToString(),
            Birthdate = ["ge1999-02-21"],
            AddressPostcode = "DN10 4PD"
        };
        
        var json = JsonSerializer.Serialize(query.ToDictionary());
        
        output.WriteLine($"SearchQuery: {json}");
        
        // Act
        var response = await httpClient.PostAsJsonAsync("/api/v1/search", query);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<SearchResult>();
        
        // Assert
        Assert.Equal(SearchResult.ResultType.MultiMatched, result!.Type);
        Assert.Null(result.NhsNumber);
        Assert.Null(result.ErrorMessage);
    }

    private async Task<DistributedApplication> StartExternalApi()
    {
        var appHost =
            await DistributedApplicationTestingBuilder
                .CreateAsync<Projects.External>(args:
                [
                    "--ConnectionStrings:secrets=http://secrets"
                ]);
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler(options =>
            {
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromHours(5);
                options.CircuitBreaker.BreakDuration = TimeSpan.FromHours(5);
                options.TotalRequestTimeout = new HttpTimeoutStrategyOptions
                {
                    Timeout = TimeSpan.FromHours(5)
                };
            });
        });

        var app = await appHost.BuildAsync();
        await app.StartAsync();

        var resourceNotificationService =
            app.Services.GetRequiredService<ResourceNotificationService>();
        await resourceNotificationService
            .WaitForResourceAsync("external-api", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(180));

        return app;
    }
}