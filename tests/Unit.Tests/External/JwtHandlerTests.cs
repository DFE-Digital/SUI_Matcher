using ExternalApi.Util;

using Unit.Tests.Util;

namespace Unit.Tests.External;

public class JwtHandlerTests
{
    [Fact]
    public void GenerateJwt_ReturnsValidToken_WhenGivenValidRsaKey()
    {

        const string audience = "test-audience";
        const string clientId = "test-client";
        const string kid = "test-kid";

        var handler = new JwtHandler();

        var token = handler.GenerateJwt(JwtPemHelper.CreateTestPem(), audience, clientId, kid);

        var handlerJwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handlerJwt.ReadJwtToken(token);

        Assert.Equal(clientId, jwt.Issuer);
        Assert.Equal(audience, jwt.Audiences.Single());
        Assert.Contains(jwt.Claims, c => c.Type == "sub" && c.Value == clientId);
    }

    [Fact]
    public void Constructor_ThrowsException_WhenKeyIsEmpty()
    {
        var service = new JwtHandler();
        Assert.Throws<InvalidOperationException>(() => service.GenerateJwt("", "", "", ""));
    }

    [Fact]
    public void GenerateJwt_RespectsExpirationTime()
    {
        var handler = new JwtHandler();
        var token = handler.GenerateJwt(JwtPemHelper.CreateTestPem(), "aud", "cid", "kid", 10);

        var handlerJwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handlerJwt.ReadJwtToken(token);

        var exp = jwt.ValidTo;
        var now = DateTime.UtcNow;

        Assert.True((exp - now).TotalMinutes <= 10.1 && (exp - now).TotalMinutes > 9);
    }
}