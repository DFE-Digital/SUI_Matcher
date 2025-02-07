namespace ExternalApi.Services;

public class AuthServiceClient(HttpClient httpClient)
{
    public async Task<string> GetToken()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/get-token");

        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }
}