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

    public TokenService(IConfiguration configuration, ILogger<TokenService> logger)
    {
        _logger = logger;

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
            throw new Exception("Token service not initialised");
        }

        if (_accessToken != null && _accessTokenExpiration > DateTimeOffset.UtcNow)
        {
            _logger.LogDebug("Found existing none expired access token found");

            return _accessToken;
        }

        _logger.LogDebug("Getting new access token from Nhs Digital oauth2 endpoint");

        // Perform request to NHS auth endpoint
        var auth = new AuthClientCredentials(_tokenUrl, _privateKey!, _clientId!, _kid!);
        _accessTokenExpiration = DateTimeOffset.UtcNow.AddMinutes(_accountTokenExpiresInMinutes);
        _accessToken = await auth.AccessToken(_accountTokenExpiresInMinutes) ??
                          throw new Exception("Failed to get access token");

        return _accessToken;
    }

    public async Task Initialise()
    {
        _privateKey = await GetSecretMaterial(NhsDigitalKeyConstants.PrivateKey) ?? throw new Exception("Failed to get private key");
        _clientId = await GetSecretMaterial(NhsDigitalKeyConstants.ClientId) ?? throw new Exception("Failed to get client id");
        _kid = await GetSecretMaterial(NhsDigitalKeyConstants.Kid) ?? throw new Exception("Failed to get kid");
    }

    protected virtual async Task<string?> GetSecretMaterial(string secretName)
    {
        KeyVaultSecret secret = await _secretClient.GetSecretAsync(secretName);
        return secret.Value;
    }
}