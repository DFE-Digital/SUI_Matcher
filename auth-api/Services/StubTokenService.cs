using AuthApi.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using System;

namespace AuthApi.Services;

public class StubTokenService(IConfiguration configuration, 
    IDistributedCache cache) : TokenService(configuration, cache)
{
    
    protected override async Task<string?> GetSecretMaterial(string secretName)
    {
        switch (secretName)
        {
            case NhsDigitalKeyConstants.ClientId:
                return await Task.FromResult(Environment.GetEnvironmentVariable("NHS_DIGITAL_CLIENT_ID"));
            case NhsDigitalKeyConstants.Kid:
                return await Task.FromResult(Environment.GetEnvironmentVariable("NHS_DIGITAL_KID"));
            case NhsDigitalKeyConstants.PrivateKey:
                return await Task.FromResult(Environment.GetEnvironmentVariable("NHS_DIGITAL_PRIVATE_KEY"));
            default:
                return null;
        }
    }
}
