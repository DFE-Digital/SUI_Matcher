using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Shared;
using Shared.Models;
using SUI.Client.Core.Application.Interfaces;

namespace SUI.Client.Core.Infrastructure.Http;

public sealed class MatchingApiClient(HttpClient httpClient) : IMatchingApiClient
{
    private const string MatchPersonPath = "api/v1/matchperson";
    private const string ReconciliationPath = "api/v1/reconciliation";

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
            BuildRequestUri(MatchPersonPath),
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

    public async Task<ReconciliationResponse?> ReconcilePersonAsync(
        ReconciliationRequest payload,
        CancellationToken cancellationToken
    )
    {
        var response = await httpClient.PostAsJsonAsync(
            BuildRequestUri(ReconciliationPath),
            payload,
            cancellationToken
        );

        var reason = await response.Content.ReadAsStringAsync(cancellationToken);
        if (reason.Contains(SharedConstants.SearchStrategy.VersionErrorMessagePrefix))
        {
            throw new NotSupportedException(reason);
        }

        ReconciliationResponse? dto = null;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            try
            {
                dto = JsonSerializer.Deserialize<ReconciliationResponse>(
                    reason,
                    JsonSerializerOptions
                );
            }
            catch (JsonException)
            {
                dto = null;
            }
        }

        if (response.IsSuccessStatusCode && dto is not null)
        {
            return dto;
        }

        if (dto?.Status == ReconciliationStatus.Error)
        {
            return dto;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Reconciliation API request failed with status code {(int)response.StatusCode} ({response.ReasonPhrase})."
            );
        }

        return null;
    }

    private Uri BuildRequestUri(string relativePath)
    {
        if (httpClient.BaseAddress is null)
        {
            return new Uri(relativePath, UriKind.Relative);
        }

        var baseAddress = httpClient.BaseAddress.ToString();
        if (!baseAddress.EndsWith('/'))
        {
            baseAddress += "/";
        }

        return new Uri(new Uri(baseAddress), relativePath);
    }
}
