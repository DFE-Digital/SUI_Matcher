using SUI.Client.Core.Models;
using System.Net.Http.Json;

namespace SUI.Client.Core.Integration;

public interface IMatchPersonApiService
{
    Task<PersonMatchResponse?> MatchPersonAsync(object payload);
}

public class MatchPersonApiService(HttpClient httpClient) : IMatchPersonApiService
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<PersonMatchResponse?> MatchPersonAsync(object payload)
    {
        var response = await _httpClient.PostAsJsonAsync("/matching/api/v1/matchperson", payload);
        //response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<PersonMatchResponse>();
        return dto;
    }
}
