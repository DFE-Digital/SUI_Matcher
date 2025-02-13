using System.Text;
using AuthApi.Lib;
using AuthApi.Models;
using Microsoft.Extensions.Caching.Distributed;

namespace AuthApi.Services;

using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using System;

public class TokenService : ITokenService
{
    // Keyvault Client
    private readonly SecretClient _secretClient;
    // Redis Client
    private readonly IDistributedCache _distributedCache;
    private readonly string _tokenUrl;

    public TokenService(IConfiguration configuration, 
        IDistributedCache cache)
    {
        var keyVaultUri = new Uri(configuration["ConnectionStrings:secrets"]!);
        _secretClient = new SecretClient(keyVaultUri, new DefaultAzureCredential());
        _distributedCache = cache;
        _tokenUrl = configuration["NhsAuthConfig:NHS_DIGITAL_TOKEN_URL"]!;
    }

    public async Task<string> GetBearerToken()
    {
        var existingAccessToken = await _distributedCache
            .GetStringAsync(NhsDigitalKeyConstants.AccountToken);

        if (existingAccessToken != null)
        {
            return existingAccessToken;
        }
        
        // Perform request to NHS auth endpoint
        var privateKey = await GetSecretMaterial(NhsDigitalKeyConstants.PrivateKey);
        var clientId = await GetSecretMaterial(NhsDigitalKeyConstants.ClientId);
        var kid = await GetSecretMaterial(NhsDigitalKeyConstants.Kid);
        
        var auth = new AuthClientCredentials(_tokenUrl, privateKey!, clientId!, kid!);
        var accessToken = await auth.AccessToken(NhsDigitalKeyConstants.AccountTokenExpiresInMinutes) ?? 
                          throw new Exception("Failed to get access token");
        
        // Store access token in Redis
        await _distributedCache.SetStringAsync(NhsDigitalKeyConstants.AccountToken, 
            accessToken, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(
                NhsDigitalKeyConstants.AccountTokenRedisStorageExpiresInMinutes)
        });
        
        return accessToken;
    }

    protected virtual async Task<string?> GetSecretMaterial(string secretName)
    {
        KeyVaultSecret secret = await _secretClient.GetSecretAsync(secretName);
        return secret.Value;
    }
}
