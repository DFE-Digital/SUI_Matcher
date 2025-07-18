using System.Net;
using System.Text.Json;

using MatchingApi;

using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Web;

using Moq;
using Moq.Protected;

namespace Unit.Tests.Matching;

public class DownstreamApiAuthHandlerTests
{
    [Fact]
    public async Task AddsBearerTokenToRequest()
    {
        var scope = "api:djkhdfhdj/.default";
        var token = "1234-1234-1234-1234";
        var tokenAcquisition = new Mock<ITokenAcquisition>();
        tokenAcquisition.Setup(x =>
            x.GetAccessTokenForAppAsync(scope, It.IsAny<string?>(), It.IsAny<TokenAcquisitionOptions>()))
            .ReturnsAsync(token);
        var configSection = new Mock<IConfigurationSection>();
        configSection.Setup(x => x.Value).Returns(scope);
        var config = new Mock<IConfiguration>();
        config.Setup(x => x.GetSection("AzureAdMatching:Scopes")).Returns(configSection.Object);
        var request = new HttpRequestMessage();

        var innerHandlerMock = new Mock<DelegatingHandler>(MockBehavior.Strict);
        innerHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", request, ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)).Verifiable();

        var handler = new DownstreamApiAuthHandler(tokenAcquisition.Object, config.Object)
        {
            InnerHandler = innerHandlerMock.Object
        };
        var invoker = new HttpMessageInvoker(handler);

        await invoker.SendAsync(request, CancellationToken.None);
        innerHandlerMock.Protected().Verify("SendAsync",
            Times.Exactly(1),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Headers.GetValues("Authorization").FirstOrDefault() == $"Bearer {token}"
            ),
            ItExpr.IsAny<CancellationToken>());
    }
}