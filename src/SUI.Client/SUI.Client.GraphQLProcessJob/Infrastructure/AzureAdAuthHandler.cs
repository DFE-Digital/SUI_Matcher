using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Options;

namespace SUI.Client.GraphQLProcessJob.Infrastructure;

public class AzureAdAuthHandler : DelegatingHandler
{
    private readonly GraphQlProcessJobOptions _options;
    private readonly HttpClient _tokenClient;
    private string? _cachedToken;
    private DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public AzureAdAuthHandler(IOptions<GraphQlProcessJobOptions> options, IHttpClientFactory httpClientFactory)
    {
        _options = options.Value;
        // Use a separate HttpClient for token requests to avoid circular dependency
        _tokenClient = httpClientFactory.CreateClient("AzureAdTokenClient");
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync(cancellationToken);
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string?> GetTokenAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_cachedToken) && DateTimeOffset.UtcNow < _tokenExpiry)
        {
            return _cachedToken;
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // Re-check inside semaphore
            if (!string.IsNullOrEmpty(_cachedToken) && DateTimeOffset.UtcNow < _tokenExpiry)
            {
                return _cachedToken;
            }

            if (string.IsNullOrEmpty(_options.TenantId) ||
                string.IsNullOrEmpty(_options.ClientId) ||
                string.IsNullOrEmpty(_options.ClientSecret))
            {
                throw new InvalidOperationException("Azure AD configuration is incomplete. TenantId, ClientId, and ClientSecret are required.");
            }

            var tokenUrl = $"https://login.microsoftonline.com/{_options.TenantId}/oauth2/v2.0/token";
            var scope = $"api://{_options.ClientId}/.default";

            var requestParams = new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "client_id", _options.ClientId },
                { "client_secret", _options.ClientSecret },
                { "scope", scope }
            };

            using var requestContent = new FormUrlEncodedContent(requestParams);
            var response = await _tokenClient.PostAsync(tokenUrl, requestContent, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException($"Failed to retrieve Azure AD token. Status: {response.StatusCode}, Details: {errorContent}");
            }

            var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var tokenResponse = await JsonSerializer.DeserializeAsync<TokenResponse>(responseStream, cancellationToken: cancellationToken);

            if (tokenResponse?.AccessToken != null)
            {
                _cachedToken = tokenResponse.AccessToken;
                // Cache token, subtracting a 1-minute buffer for expiration
                var expiresInSeconds = tokenResponse.ExpiresIn ?? 3600;
                _tokenExpiry = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds).AddMinutes(-1);
            }
        }
        finally
        {
            _semaphore.Release();
        }

        return _cachedToken;
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int? ExpiresIn { get; set; }
    }
}