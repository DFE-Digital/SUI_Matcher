using System.Net;
using System.Net.Http.Json;
using Moq;
using Moq.Protected;
using Shared;
using Shared.Models;
using SUI.Client.Core.Infrastructure.Http;

namespace Unit.Tests.Client.CoreTests.InfrastructureTests;

public class MatchingApiClientTests
{
    [Fact]
    public async Task Should_PostSearchSpecification_When_RequestIsValid()
    {
        SearchSpecification? sentPayload = null;
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Returns<HttpRequestMessage, CancellationToken>(
                async (request, _) =>
                {
                    sentPayload = await request.Content!.ReadFromJsonAsync<SearchSpecification>();
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = JsonContent.Create(
                            new PersonMatchResponse
                            {
                                Result = new MatchResult { MatchStatus = MatchStatus.Match },
                            }
                        ),
                    };
                }
            );

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost:5000/"),
        };
        var sut = new MatchingApiClient(httpClient);

        await sut.MatchPersonAsync(
            new SearchSpecification
            {
                Given = "Jane",
                Family = "Doe",
                BirthDate = new DateOnly(2012, 5, 10),
                SearchStrategy = SharedConstants.SearchStrategy.Strategies.Strategy4,
                StrategyVersion = 2,
            },
            CancellationToken.None
        );

        Assert.NotNull(sentPayload);
        Assert.Equal("Jane", sentPayload!.Given);
        Assert.Equal(
            SharedConstants.SearchStrategy.Strategies.Strategy4,
            sentPayload.SearchStrategy
        );
        Assert.Equal(2, sentPayload.StrategyVersion);
        handlerMock
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post
                    && req.RequestUri != null
                    && req.RequestUri.ToString() == "http://localhost:5000/api/v1/matchperson"
                ),
                ItExpr.IsAny<CancellationToken>()
            );
    }

    [Theory]
    [InlineData("http://localhost:5000", "http://localhost:5000/api/v1/matchperson")]
    [InlineData("http://localhost:5000/", "http://localhost:5000/api/v1/matchperson")]
    [InlineData(
        "http://localhost:5000/matching",
        "http://localhost:5000/matching/api/v1/matchperson"
    )]
    [InlineData(
        "http://localhost:5000/matching/",
        "http://localhost:5000/matching/api/v1/matchperson"
    )]
    public async Task Should_PreserveRouteRoot_When_BaseAddressTrailingSlashVaries(
        string baseAddress,
        string expectedRequestUri
    )
    {
        Uri? requestUri = null;
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Returns<HttpRequestMessage, CancellationToken>(
                (request, _) =>
                {
                    requestUri = request.RequestUri;
                    return Task.FromResult(
                        new HttpResponseMessage
                        {
                            StatusCode = HttpStatusCode.OK,
                            Content = JsonContent.Create(new PersonMatchResponse()),
                        }
                    );
                }
            );

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri(baseAddress) };
        var sut = new MatchingApiClient(httpClient);

        await sut.MatchPersonAsync(new SearchSpecification(), CancellationToken.None);

        // A leading slash in the client path would drop the configured /matching route root.
        Assert.Equal(expectedRequestUri, requestUri?.ToString());
    }

    [Fact]
    public async Task Should_ThrowNotSupportedException_When_StrategyVersionIsInvalid()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(
                new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent(
                        SharedConstants.SearchStrategy.VersionErrorMessagePrefix
                    ),
                }
            );

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost:5000/"),
        };
        var sut = new MatchingApiClient(httpClient);

        await Assert.ThrowsAsync<NotSupportedException>(() =>
            sut.MatchPersonAsync(new SearchSpecification(), CancellationToken.None)
        );
    }

    [Fact]
    public async Task Should_ThrowHttpRequestException_When_NonSuccessResponseHasNoExpectedBody()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(
                new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.TooManyRequests,
                    ReasonPhrase = "Too Many Requests",
                    Content = new StringContent("rate limited"),
                }
            );

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost:5000/"),
        };
        var sut = new MatchingApiClient(httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            sut.MatchPersonAsync(new SearchSpecification(), CancellationToken.None)
        );
    }

    [Fact]
    public async Task Should_PostReconciliationRequestToReconciliationEndpoint()
    {
        ReconciliationRequest? sentPayload = null;
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Returns<HttpRequestMessage, CancellationToken>(
                async (request, cancellationToken) =>
                {
                    sentPayload = await request.Content!.ReadFromJsonAsync<ReconciliationRequest>(
                        cancellationToken
                    );
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = JsonContent.Create(
                            new ReconciliationResponse
                            {
                                Status = ReconciliationStatus.NoDifferences,
                            }
                        ),
                    };
                }
            );
        var sut = new MatchingApiClient(
            new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("http://localhost:5000/matching/"),
            }
        );

        var result = await sut.ReconcilePersonAsync(
            new ReconciliationRequest { NhsNumber = "9999999993", Given = "Jane" },
            CancellationToken.None
        );

        Assert.NotNull(sentPayload);
        Assert.Equal("9999999993", sentPayload!.NhsNumber);
        Assert.Equal(ReconciliationStatus.NoDifferences, result!.Status);
        handlerMock
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(request =>
                    request.RequestUri!.ToString()
                    == "http://localhost:5000/matching/api/v1/reconciliation"
                ),
                ItExpr.IsAny<CancellationToken>()
            );
    }
}
