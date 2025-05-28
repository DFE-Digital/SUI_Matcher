using System.Security.Cryptography;

using Azure.Security.KeyVault.Secrets;

using ExternalApi.Util;

using Microsoft.Extensions.Options;

namespace ExternalApi.Services;

public class StubTokenService(
    IOptions<NhsAuthConfigOptions> options,
    ILogger<StubTokenService> logger, IJwtHandler jwtHandler, IHttpClientFactory httpClientFactory, SecretClient secretClient) :
    TokenService(options, logger, jwtHandler, httpClientFactory, secretClient)
{
    private readonly IOptions<NhsAuthConfigOptions> _options = options;

    protected override async Task<string?> GetSecretMaterial(string secretName)
    {
        switch (secretName)
        {
            case NhsDigitalKeyConstants.ClientId:
                return await Task.FromResult(_options.Value.NHS_DIGITAL_CLIENT_ID);
            case NhsDigitalKeyConstants.Kid:
                return await Task.FromResult(_options.Value.NHS_DIGITAL_KID);
            case NhsDigitalKeyConstants.PrivateKey:
                var privateKey = _options.Value.NHS_DIGITAL_PRIVATE_KEY;
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