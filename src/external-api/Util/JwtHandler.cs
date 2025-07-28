using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

using Microsoft.IdentityModel.Tokens;

namespace ExternalApi.Util;

public interface IJwtHandler
{
    string GenerateJwt(string keyOrPfx, string audience, string clientId, string kid, int expInMinutes = 1);
}

public class JwtHandler : IJwtHandler
{
    public string GenerateJwt(string keyOrPfx, string audience, string clientId, string kid, int expInMinutes = 1)
    {
        if (keyOrPfx.Length <= 0)
        {
            throw new InvalidOperationException("Can not recognise the certificate/key extension");
        }

        SigningCredentials signingCredentials = FromPrivateKey(keyOrPfx, kid);
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            clientId,
            audience,
            new List<Claim>
            {
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Sub, clientId),
            },
            now,
            now.AddMinutes(expInMinutes),
            signingCredentials
        );

        var tokenHandler = new JwtSecurityTokenHandler();

        return tokenHandler.WriteToken(token);
    }

    private static SigningCredentials FromPrivateKey(string privateKey, string kid)
    {
        privateKey = privateKey.Replace("-----BEGIN RSA PRIVATE KEY-----", "");
        privateKey = privateKey.Replace("-----END RSA PRIVATE KEY-----", "");
        var keyBytes = Convert.FromBase64String(privateKey);

        var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(keyBytes, out _);

        var rsaSecurityKey = new RsaSecurityKey(rsa)
        {
            KeyId = kid
        };

        return new SigningCredentials(rsaSecurityKey, SecurityAlgorithms.RsaSha512)
        {
            CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
        };
    }
}