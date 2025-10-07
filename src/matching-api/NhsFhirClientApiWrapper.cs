using Shared.Endpoint;
using Shared.Models;

namespace MatchingApi;

public class NhsFhirClientApiWrapper(HttpClient httpClient) : INhsFhirClient
{
    public async Task<SearchResult?> PerformSearch(SearchQuery query)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/search")
        {
            Content = JsonContent.Create(query)
        };
        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<SearchResult>();
    }

    public async Task<DemographicResult> PerformSearchByNhsId(string? nhsId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/demographics")
        {
            Content = JsonContent.Create(new DemographicRequest { NhsNumber = nhsId })
        };
        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<DemographicResult>();

        if (result == null)
        {
            throw new InvalidOperationException("Failed to deserialize DemographicResult from the response.");
        }

        return result;
    }
}