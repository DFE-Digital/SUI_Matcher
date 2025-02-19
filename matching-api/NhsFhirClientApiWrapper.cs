using Shared.Models;
using SUI.Core.Endpoints;

namespace MatchingApi;

public class NhsFhirClientApiWrapper (HttpClient httpClient) : INhsFhirClient
{
    public async Task<SearchResult?> PerformSearch(SearchQuery searchQuery)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/search")
        {
            Content = JsonContent.Create(searchQuery)
        };
        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<SearchResult>();
    }

}