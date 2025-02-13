using AuthApi.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using System.Security.Cryptography;


namespace AuthApi.Services;

public class StubTokenService(IConfiguration configuration, 
    IDistributedCache cache) : TokenService(configuration, cache)
{
    private readonly IConfiguration _configuration = configuration;

    protected override async Task<string?> GetSecretMaterial(string secretName)
    {
        switch (secretName)
        {
            case NhsDigitalKeyConstants.ClientId:
                return await Task.FromResult(_configuration["NhsAuthConfig:NHS_DIGITAL_CLIENT_ID"]);
            case NhsDigitalKeyConstants.Kid:
                return await Task.FromResult(_configuration["NhsAuthConfig:NHS_DIGITAL_KID"]);
            case NhsDigitalKeyConstants.PrivateKey:
                var privateKey = _configuration["NhsAuthConfig:NHS_DIGITAL_PRIVATE_KEY"];
                Console.WriteLine($"Private key: {privateKey}");
            if (string.IsNullOrEmpty(privateKey))
            {
                using (var rsa = RSA.Create(2048))
                {
                    privateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
                }
            }
            return await Task.FromResult(privateKey);
            default:
                return null;
        }
    }
}
