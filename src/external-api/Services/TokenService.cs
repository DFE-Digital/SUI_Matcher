using System.Net;
using System.Text.Json.Nodes;

using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

using ExternalApi.Util;

namespace ExternalApi.Services;

public interface ITokenService
{
    Task<string> GetBearerToken();

    Task Initialise();
}

public class TokenService : ITokenService
{
    protected static class NhsDigitalKeyConstants
    {
        public const string ClientId = "nhs-digital-client-id";
        public const string PrivateKey = "nhs-digital-private-key";
        public const string Kid = "nhs-digital-kid";
        public const int AccountTokenExpiresInMinutes = 5;
    }

    // Key vault Client
    private readonly SecretClient _secretClient;
    private readonly string _tokenUrl;
    private readonly int _accountTokenExpiresInMinutes;
    private string? _privateKey;
    private string? _clientId;
    private string? _kid;

    private string? _accessToken;
    private DateTimeOffset _accessTokenExpiration;
    private readonly ILogger<TokenService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IJwtHandler _jwtHandler;

    public TokenService(IConfiguration configuration, ILogger<TokenService> logger, IJwtHandler jwtHandler, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("nhs-auth-api");
        _jwtHandler = jwtHandler;
        
        var keyVaultUri = new Uri(configuration["ConnectionStrings:secrets"]!);
        _secretClient = new SecretClient(keyVaultUri, new DefaultAzureCredential());
        _tokenUrl = configuration["NhsAuthConfig:NHS_DIGITAL_TOKEN_URL"]!;

        var parsed = int.TryParse(configuration["NhsAuthConfig:NHS_DIGITAL_ACCESS_TOKEN_EXPIRES_IN_MINUTES"],
            out _accountTokenExpiresInMinutes);

        if (!parsed)
        {
            _accountTokenExpiresInMinutes = NhsDigitalKeyConstants.AccountTokenExpiresInMinutes;
        }
    }

    public async Task<string> GetBearerToken()
    {
        if (_privateKey == null || _clientId == null || _kid == null)
        {
            throw new InvalidOperationException("Token service not initialised");
        }

        if (_accessToken != null && _accessTokenExpiration > DateTimeOffset.UtcNow)
        {
            _logger.LogDebug("Found existing none expired access token found");

            return _accessToken;
        }

        _logger.LogDebug("Getting new access token from Nhs Digital oauth2 endpoint");

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
        KeyVaultSecret secret = await _secretClient.GetSecretAsync(secretName);
        return secret.Value;
    }
    
    public async Task<string?> AccessToken(int expInMinutes = 1)
    {
        var jwt = _jwtHandler.GenerateJwt(_privateKey!, _tokenUrl, _clientId!, _kid!, expInMinutes);

        var values = new Dictionary<string, string>
        {
            {"grant_type", "client_credentials"},
            {"client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"},
            {"client_assertion", jwt},
        };
        var content = new FormUrlEncodedContent(values);

        Console.WriteLine("Requesting token from " + _tokenUrl);

        var response = await _httpClient.PostAsync(_tokenUrl, content);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new HttpRequestException("Authentication failed. \n" + response.Content);
        }

        var resBody = await response.Content.ReadAsStringAsync();
        var parsed = JsonNode.Parse(resBody);

        return parsed?["access_token"]?.ToString();

    }
}