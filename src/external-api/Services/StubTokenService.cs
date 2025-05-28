using System.Security.Cryptography;

using ExternalApi.Util;

namespace ExternalApi.Services;

public class StubTokenService(
    IConfiguration configuration, ILogger<StubTokenService> logger, IJwtHandler jwtHandler, IHttpClientFactory httpClientFactory) :
    TokenService(configuration, logger, jwtHandler, httpClientFactory)
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
                if (string.IsNullOrEmpty(privateKey))
                {
                    using var rsa = RSA.Create(2048);
                    privateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
                }
                else if (privateKey.StartsWith("file:"))
                {
                    privateKey = privateKey.Replace("file:", "");
                    privateKey = await File.ReadAllTextAsync(privateKey);
                }
                return await Task.FromResult(privateKey);
            default:
                return null;
        }
    }
}