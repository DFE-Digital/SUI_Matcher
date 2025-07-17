using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Shared.Models;

using SUI.Client.Core.Models;

namespace SUI.Client.Core.Integration;

public interface IMatchPersonApiService
{
    Task<PersonMatchResponse?> MatchPersonAsync(MatchPersonPayload payload);
}

public class MatchPersonApiService(HttpClient httpClient) : IMatchPersonApiService
{

    public async Task<PersonMatchResponse?> MatchPersonAsync(MatchPersonPayload payload)
    {
        var response = await httpClient.PostAsJsonAsync("/matching/api/v1/matchperson", payload);

        var options = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        };

        if (response.IsSuccessStatusCode)
        {
            var dto = await response.Content.ReadFromJsonAsync<PersonMatchResponse>(options);
            return dto;
        }
        else
        {
            Console.WriteLine(response.StatusCode);
            Console.WriteLine(response.ReasonPhrase);
            Console.WriteLine(response.Content?.ReadAsStringAsync().Result);
            return null;
        }
    }
}