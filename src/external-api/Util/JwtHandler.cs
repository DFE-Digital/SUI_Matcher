using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

using IdentityModel;

using Microsoft.IdentityModel.Tokens;

namespace ExternalApi.Util;

public class JwtHandler
{
    private readonly string _audience;
    private readonly string _clientId;
    private readonly SigningCredentials _signingCredentials;

    public JwtHandler(string keyOrPfx, string audience, string clientId, string kid)
    {
        _audience = audience;
        _clientId = clientId;

        if (keyOrPfx.Length > 0)
        {
            _signingCredentials = FromPrivateKey(keyOrPfx, kid);
        }
        else
        {
            throw new InvalidOperationException("Can not recognise the certificate/key extension");
        }
    }

    public string GenerateJwt(int expInMinutes = 1)
    {

        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            _clientId,
            _audience,
            new List<Claim>
            {
                new("jti", Guid.NewGuid().ToString()),
                new(JwtClaimTypes.Subject, _clientId),
            },
            now,
            now.AddMinutes(expInMinutes),
            _signingCredentials
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