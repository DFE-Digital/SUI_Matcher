using SUI.Client.Core.Models;
using Shared.Models.Client;
using System.Net.Http.Json;

namespace SUI.Client.Core.Integration;

public interface IMatchPersonApiService
{
    Task<PersonMatchResponse?> MatchPersonAsync(MatchPersonPayload payload);
}

public class MatchPersonApiService(HttpClient httpClient) : IMatchPersonApiService
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<PersonMatchResponse?> MatchPersonAsync(MatchPersonPayload payload)
    {
        var response = await _httpClient.PostAsJsonAsync("/matching/api/v1/matchperson", payload);
        var dto = await response.Content.ReadFromJsonAsync<PersonMatchResponse>();
        return dto;
    }
}
