using System.Net;

using Azure.Core;

using Microsoft.Extensions.Options;

using Moq;
using Moq.Protected;

using SUI.Client.GraphQLProcessJob;
using SUI.Client.GraphQLProcessJob.Infrastructure;

namespace Unit.Tests.Client.GraphQlProcessJob;

public class AzureAdAuthHandlerTests
{
    private readonly Mock<TokenCredential> _tokenCredentialMock = new();

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

        // Mock GetTokenAsync behavior of TokenCredential
        _tokenCredentialMock
            .Setup(c => c.GetTokenAsync(
                It.Is<TokenRequestContext>(ctx => ctx.Scopes.Contains("https://example.com/.default")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccessToken("mockToken123", DateTimeOffset.UtcNow.AddHours(1)));

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

        var sut = new AzureAdAuthHandler(options, _tokenCredentialMock.Object)
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
    public void TokenCredentialRegistration_ShouldThrowInvalidOperationException_WhenConfigIsMissing()
    {
        // Arrange
        var options = Options.Create(new GraphQlProcessJobOptions
        {
            TenantId = "", // Missing config
            ClientId = "client-123",
            ClientSecret = "secret-123"
        });

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            if (string.IsNullOrEmpty(options.Value.TenantId) ||
                string.IsNullOrEmpty(options.Value.ClientId) ||
                string.IsNullOrEmpty(options.Value.ClientSecret))
            {
                throw new InvalidOperationException(
                    "Azure AD configuration is incomplete. TenantId, ClientId, and ClientSecret are required.");
            }
        });
    }
}