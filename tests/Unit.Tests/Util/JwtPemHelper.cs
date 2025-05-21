namespace Unit.Tests.Util;

public static class JwtPemHelper
{
    public static string CreateTestPem()
    {
        // Generate a new RSA key for testing
        using var rsa = System.Security.Cryptography.RSA.Create(2048);
        var privateKeyBytes = rsa.ExportRSAPrivateKey();
        var privateKeyBase64 = Convert.ToBase64String(privateKeyBytes);
        return $"-----BEGIN RSA PRIVATE KEY-----{privateKeyBase64}-----END RSA PRIVATE KEY-----";
    }
}