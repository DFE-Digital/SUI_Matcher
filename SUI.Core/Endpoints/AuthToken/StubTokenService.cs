﻿using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace SUI.Core.Endpoints.AuthToken;

public class StubTokenService(
    IConfiguration configuration, ILogger<StubTokenService> logger) : 
    TokenService(configuration, logger)
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