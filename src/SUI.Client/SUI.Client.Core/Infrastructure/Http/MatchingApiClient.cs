using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Shared;
using Shared.Models;
using SUI.Client.Core.Application.Interfaces;

namespace SUI.Client.Core.Infrastructure.Http;

public sealed class MatchingApiClient(HttpClient httpClient) : IMatchingApiClient
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true,
    };

    public async Task<PersonMatchResponse?> MatchPersonAsync(
        SearchSpecification payload,
        CancellationToken cancellationToken
    )
    {
        var response = await httpClient.PostAsJsonAsync(
            "/matching/api/v1/matchperson",
            payload,
            cancellationToken
        );

        var reason = await response.Content.ReadAsStringAsync(cancellationToken);
        if (reason.Contains(SharedConstants.SearchStrategy.VersionErrorMessagePrefix))
        {
            throw new NotSupportedException(reason);
        }

        PersonMatchResponse? dto = null;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            try
            {
                dto = JsonSerializer.Deserialize<PersonMatchResponse>(
                    reason,
                    JsonSerializerOptions
                );
            }
            catch (JsonException)
            {
                dto = null;
            }
        }

        if (dto is not null)
        {
            return dto;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Matching API request failed with status code {(int)response.StatusCode} ({response.ReasonPhrase})."
            );
        }

        return null;
    }
}
