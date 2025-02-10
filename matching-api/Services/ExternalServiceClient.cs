namespace MatchingApi.Services;

public class ExternalServiceClient(HttpClient httpClient)
{
    public async Task<string> PerformQuery()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/perform-search");
        
        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }
}