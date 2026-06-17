using System.Net;
using Azure.Security.KeyVault.Secrets;
using ExternalApi;
using ExternalApi.Services;
using ExternalApi.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Unit.Tests.External.Services;

public class StubTokenServiceTests
{
    [Fact]
    public async Task Should_ReturnBearerToken_When_StubPrivateKeyConfigurationIsEmpty()
    {
        // This test is proving that the stub key generated works with JwtHandler.

        var handler = new MockHttpMessageHandler(
            (_, _) =>
                Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{\"access_token\":\"stub-token\"}"),
                    }
                )
        );
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://auth.example.com/"),
        };

        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var options = Options.Create(
            new NhsAuthConfigOptions
            {
                NHS_DIGITAL_CLIENT_ID = "test-client",
                NHS_DIGITAL_KID = "test-kid",
                NHS_DIGITAL_PRIVATE_KEY = "",
                NHS_DIGITAL_ACCESS_TOKEN_EXPIRES_IN_MINUTES = 5,
            }
        );

        var service = new StubTokenService(
            options,
            Mock.Of<ILogger<StubTokenService>>(),
            new JwtHandler(),
            httpClientFactoryMock.Object,
            Mock.Of<SecretClient>()
        );

        await service.Initialise();

        var token = await service.GetBearerToken();

        Assert.Equal("stub-token", token);
    }
}
