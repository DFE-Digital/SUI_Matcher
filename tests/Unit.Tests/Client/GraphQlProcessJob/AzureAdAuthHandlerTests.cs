using System.Net;
using System.Net.Http.Headers;

using Microsoft.Extensions.Options;

using Moq;
using Moq.Protected;

using SUI.Client.GraphQLProcessJob;
using SUI.Client.GraphQLProcessJob.Infrastructure;

namespace Unit.Tests.Client.GraphQlProcessJob;

public class AzureAdAuthHandlerTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<HttpMessageHandler> _tokenHttpHandlerMock;

    public AzureAdAuthHandlerTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _tokenHttpHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        var tokenClient = new HttpClient(_tokenHttpHandlerMock.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient("AzureAdTokenClient")).Returns(tokenClient);
    }

    [Fact]
    public async Task SendAsync_ShouldAddBearerToken_WhenTokenRequestSucceeds()
    {
        // Arrange
        var options = Options.Create(new GraphQlProcessJobOptions
        {
            TenantId = "tenant-123",
            ClientId = "client-123",
            ClientSecret = "secret-123",
            Scope = "https://example.com/.default"
        });

        // Mock token endpoint call
        _tokenHttpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("https://login.microsoftonline.com/tenant-123/oauth2/v2.0/token")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{ \"access_token\": \"mockToken123\", \"expires_in\": 3600 }", System.Text.Encoding.UTF8, "application/json")
            });

        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/data");

        var innerHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        innerHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                request,
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var sut = new AzureAdAuthHandler(options, _httpClientFactoryMock.Object)
        {
            InnerHandler = innerHandlerMock.Object
        };

        var invoker = new HttpMessageInvoker(sut);

        // Act
        await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(request.Headers.Authorization);
        Assert.Equal("Bearer", request.Headers.Authorization.Scheme);
        Assert.Equal("mockToken123", request.Headers.Authorization.Parameter);

        innerHandlerMock.Protected().Verify("SendAsync", Times.Once(), request, ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_ShouldUseCachedToken_OnConsecutiveRequests()
    {
        // Arrange
        var options = Options.Create(new GraphQlProcessJobOptions
        {
            TenantId = "tenant-123",
            ClientId = "client-123",
            ClientSecret = "secret-123",
            Scope = "https://example.com/.default"
        });

        // Mock token endpoint call (should only be hit once due to caching)
        _tokenHttpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{ \"access_token\": \"cachedToken123\", \"expires_in\": 3600 }", System.Text.Encoding.UTF8, "application/json")
            });

        var request1 = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/data1");
        var request2 = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/data2");

        var innerHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        innerHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var sut = new AzureAdAuthHandler(options, _httpClientFactoryMock.Object)
        {
            InnerHandler = innerHandlerMock.Object
        };

        var invoker = new HttpMessageInvoker(sut);

        // Act
        await invoker.SendAsync(request1, CancellationToken.None);
        await invoker.SendAsync(request2, CancellationToken.None);

        // Assert
        Assert.Equal("cachedToken123", request1.Headers.Authorization?.Parameter);
        Assert.Equal("cachedToken123", request2.Headers.Authorization?.Parameter);

        // Verify token endpoint was requested exactly once
        _tokenHttpHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task SendAsync_ShouldThrowInvalidOperationException_WhenConfigIsMissing()
    {
        // Arrange
        var options = Options.Create(new GraphQlProcessJobOptions
        {
            TenantId = "", // Missing config
            ClientId = "client-123",
            ClientSecret = "secret-123"
        });

        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/data");

        var innerHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var sut = new AzureAdAuthHandler(options, _httpClientFactoryMock.Object)
        {
            InnerHandler = innerHandlerMock.Object
        };

        var invoker = new HttpMessageInvoker(sut);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => invoker.SendAsync(request, CancellationToken.None));
    }
}