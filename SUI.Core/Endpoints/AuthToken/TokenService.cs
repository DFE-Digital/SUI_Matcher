using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;

namespace SUI.Core.Endpoints.AuthToken;

public interface ITokenService
{
    Task<string> GetBearerToken();

    Task Initialise();
}

public class TokenService : ITokenService
{
    public static class NhsDigitalKeyConstants
    {
        public const string ClientId = "nhs-digital-client-id";
        public const string PrivateKey = "nhs-digital-private-key";
        public const string Kid = "nhs-digital-kid";
        public const int AccountTokenExpiresInMinutes = 5;
    }

    // Keyvault Client
    private readonly SecretClient _secretClient;
    private readonly string _tokenUrl;
    private readonly int _accountTokenExpiresInMinutes;
    private string _privateKey;
    private string _clientId;
    private string _kid;
    
    private string? _accessToken;
    private DateTimeOffset _accessTokenExpiration;

    public TokenService(IConfiguration configuration)
    {
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
        if (_accessToken != null && _accessTokenExpiration > DateTimeOffset.UtcNow)
        {
            return _accessToken;
        }
        
        // Perform request to NHS auth endpoint
        var auth = new AuthClientCredentials(_tokenUrl, _privateKey, _clientId, _kid);
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
