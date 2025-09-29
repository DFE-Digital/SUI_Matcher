using System.Net;
using System.Text.Json.Nodes;

using Azure.Security.KeyVault.Secrets;

using ExternalApi.Util;

using Microsoft.Extensions.Options;

namespace ExternalApi.Services;

public interface ITokenService
{
    Task<string> GetBearerToken();

    Task Initialise();
}

public class TokenService(
    IOptions<NhsAuthConfigOptions> options,
    ILogger<TokenService> logger,
    IJwtHandler jwtHandler,
    IHttpClientFactory httpClientFactory,
    SecretClient secretClient)
    : ITokenService
{
    protected static class NhsDigitalKeyConstants
    {
        public const string ClientId = "nhs-digital-client-id";
        public const string PrivateKey = "nhs-digital-private-key";
        public const string Kid = "nhs-digital-kid";
        public const int AccountTokenExpiresInMinutes = 5;
    }

    // Key vault Client
    private readonly int _accountTokenExpiresInMinutes = options.Value.NHS_DIGITAL_ACCESS_TOKEN_EXPIRES_IN_MINUTES ?? NhsDigitalKeyConstants.AccountTokenExpiresInMinutes;
    private string? _privateKey;
    private string? _clientId;
    private string? _kid;

    private string? _accessToken;
    private DateTimeOffset _accessTokenExpiration;
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("nhs-auth-api");

    public async Task<string> GetBearerToken()
    {
        if (_privateKey == null || _clientId == null || _kid == null)
        {
            throw new InvalidOperationException("Token service not initialised");
        }

        if (_accessToken != null && _accessTokenExpiration > DateTimeOffset.UtcNow)
        {
            logger.LogDebug("Found existing none expired access token found");

            return _accessToken;
        }

        logger.LogDebug("Getting new access token from Nhs Digital oauth2 endpoint");

        // Perform request to NHS auth endpoint
        _accessTokenExpiration = DateTimeOffset.UtcNow.AddMinutes(_accountTokenExpiresInMinutes);
        _accessToken = await AccessToken(_accountTokenExpiresInMinutes) ??
                       throw new InvalidOperationException("Failed to get access token");

        return _accessToken;
    }

    public async Task Initialise()
    {
        _privateKey = await GetSecretMaterial(NhsDigitalKeyConstants.PrivateKey) ?? throw new InvalidOperationException("Failed to get private key");
        _clientId = await GetSecretMaterial(NhsDigitalKeyConstants.ClientId) ?? throw new InvalidOperationException("Failed to get client id");
        _kid = await GetSecretMaterial(NhsDigitalKeyConstants.Kid) ?? throw new InvalidOperationException("Failed to get kid");
    }

    protected virtual async Task<string?> GetSecretMaterial(string secretName)
    {
        KeyVaultSecret secret = await secretClient.GetSecretAsync(secretName);
        return secret.Value;
    }

    private async Task<string?> AccessToken(int expInMinutes = 1)
    {
        var authAddress = _httpClient.BaseAddress!.ToString();
        var jwt = jwtHandler.GenerateJwt(_privateKey!, authAddress, _clientId!, _kid!, expInMinutes);

        var values = new Dictionary<string, string>
        {
            {"grant_type", "client_credentials"},
            {"client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"},
            {"client_assertion", jwt},
        };
        var content = new FormUrlEncodedContent(values);

        logger.LogDebug("Requesting token from " + authAddress);

        var response = await _httpClient.PostAsync(authAddress, content);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new HttpRequestException("Authentication failed. \n" + response.Content);
        }

        logger.LogInformation("Retrieved Nhs Digital FHIR API access token");

        var resBody = await response.Content.ReadAsStringAsync();
        var parsed = JsonNode.Parse(resBody);

        return parsed?["access_token"]?.ToString();
    }
}