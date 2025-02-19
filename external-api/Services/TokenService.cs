using System.Text;
using ExternalApi.Lib;
using ExternalApi.Models;
using Microsoft.Extensions.Caching.Distributed;

namespace ExternalApi.Services;

using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using System;

public class TokenService : ITokenService
{
    // Keyvault Client
    private readonly SecretClient _secretClient;
    private readonly string _tokenUrl;
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
    }

    public async Task<string> GetBearerToken()
    {
        if (_accessToken != null && _accessTokenExpiration > DateTimeOffset.UtcNow)
        {
            return _accessToken;
        }
        
        // Perform request to NHS auth endpoint
        var auth = new AuthClientCredentials(_tokenUrl, _privateKey, _clientId, _kid);
        _accessTokenExpiration = DateTimeOffset.UtcNow.AddMinutes(NhsDigitalKeyConstants.AccountTokenExpiresInMinutes);
        _accessToken = await auth.AccessToken(NhsDigitalKeyConstants.AccountTokenExpiresInMinutes) ?? 
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
