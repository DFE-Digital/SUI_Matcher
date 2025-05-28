using System.Net;
using System.Text.Json.Nodes;

using Azure;
using Azure.Security.KeyVault.Secrets;

using ExternalApi;
using ExternalApi.Services;
using ExternalApi.Util;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

namespace Unit.Tests.External.Services;

public class TokenServiceTests
{
    private readonly Mock<IConfiguration> _configMock = new();
    private readonly Mock<ILogger<TokenService>> _loggerMock = new();
    private readonly Mock<IJwtHandler> _jwtHandlerMock = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<SecretClient> _secretClientMock = new();

    private TokenService CreateService(HttpClient? httpClient = null)
    {
        _configMock.Setup(c => c["ConnectionStrings:secrets"]).Returns("https://test.vault.azure.net/");
        if (httpClient == null)
        {
            // Mock handler returns a dummy response for any request
            var handler = new MockHttpMessageHandler((_, _) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"access_token\":\"token\"}")
                }));
            httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://auth.example.com/") };
        }
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var secret = new KeyVaultSecret("name", "token");
        var responseMock = new Mock<Response<KeyVaultSecret>>();
        responseMock.Setup(r => r.Value).Returns(secret);

        _secretClientMock
            .Setup(s => s.GetSecretAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseMock.Object);

        IOptions<NhsAuthConfigOptions> options = Options.Create(new NhsAuthConfigOptions
        {
            NHS_DIGITAL_ACCESS_TOKEN_EXPIRES_IN_MINUTES = 5 // Set a default expiry for testing
        });

        return new TokenService(options, _loggerMock.Object, _jwtHandlerMock.Object, _httpClientFactoryMock.Object, _secretClientMock.Object);
    }

    [Fact]
    public async Task Initialise_SetsSecrets_WhenSecretsArePresent()
    {
        // Arrange
        var secret = new KeyVaultSecret("name", "value");
        var responseMock = new Mock<Response<KeyVaultSecret>>();
        responseMock.Setup(r => r.Value).Returns(secret);

        _secretClientMock
            .Setup(s => s.GetSecretAsync(It.IsAny<string>(), null, CancellationToken.None))
            .ReturnsAsync(responseMock.Object);

        var service = CreateService();

        // Act
        await service.Initialise();

        // Assert
        // No exception means success
    }

    [Fact]
    public async Task GetBearerToken_ReturnsSameToken_IfNotExpired()
    {
        // Arrange
        var service = CreateService();
        await service.Initialise();

        // Act
        var token1 = await service.GetBearerToken();
        var token2 = await service.GetBearerToken();

        // Assert
        Assert.Equal(token1, token2);
    }

    [Fact]
    public async Task GetBearerToken_ReturnsCachedToken_IfNotExpired()
    {
        // Arrange
        var service = CreateService();
        await service.Initialise();

        // Act
        var token = await service.GetBearerToken();

        // Assert
        Assert.Equal("token", token);
    }

    [Fact]
    public async Task GetBearerToken_Throws_IfNotInitialised()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetBearerToken());
    }

    [Fact]
    public async Task GetBearerToken_RequestsNewToken_WhenExpired()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
        {
            var obj = new JsonObject { ["access_token"] = "newtoken" };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(obj.ToJsonString())
            });
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://auth.example.com/") };

        _jwtHandlerMock.Setup(j => j.GenerateJwt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .Returns("jwt");

        var service = CreateService(httpClient);
        await service.Initialise();

        // Act
        var token = await service.GetBearerToken();

        // Assert
        Assert.Equal("newtoken", token);
    }

    [Fact]
    public async Task GetBearerToken_Throws_OnHttpError()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("fail")
            }));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://auth.example.com/") };

        _jwtHandlerMock.Setup(j => j.GenerateJwt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .Returns("jwt");

        var service = CreateService(httpClient);
        await service.Initialise();

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => service.GetBearerToken());
    }
}

// Helper for mocking HttpClient
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;
    public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) => _handler = handler;
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => _handler(request, cancellationToken);
}