using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Moq;
using Moq.Protected;

using Shared.Models;

using SUI.Client.Core.Integration;
using SUI.Client.Core.Models;

namespace Unit.Tests.Client;

public class MatchPersonApiServiceTests
{
    [Fact]
    public async Task GivenAValidRequestAndResponseReturnsCorrectDto()
    {
        // Arrange
        var validResult = new PersonMatchResponse { Result = new MatchResult { NhsNumber = "12345" } };
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(validResult),
            })
            .Verifiable();
        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost:5000/")
        };
        var validPayload = new MatchPersonPayload
        {
            AddressPostalCode = "LF123ED"
        };

        // Act
        var service = new MatchPersonApiService(httpClient);
        var result = await service.MatchPersonAsync(validPayload);

        // Assert
        Assert.Equal(validResult.Result.NhsNumber, result?.Result?.NhsNumber);
        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Exactly(1),
            ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task GivenAValidRequestAndInvalidResponseReturnsNull()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadGateway,
                ReasonPhrase = "Server Error",
                Content = new StringContent("An error occured"),
            })
            .Verifiable();
        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost:5000/")
        };
        var validPayload = new MatchPersonPayload
        {
            AddressPostalCode = "LF123ED"
        };

        // Act
        var service = new MatchPersonApiService(httpClient);
        var result = await service.MatchPersonAsync(validPayload);

        // Assert
        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Exactly(1),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post
            ),
            ItExpr.IsAny<CancellationToken>()
        );
        Assert.Null(result);
    }

    [Fact]
    public async Task ReconcilePerson_GivenAValidRequestAndResponseReturnsCorrectDto()
    {
        // Arrange
        var validResult = new ReconciliationResponse { Person = new NhsPerson { NhsNumber = "12345" } };
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(validResult),
            })
            .Verifiable();
        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost:5000/")
        };
        var validPayload = new ReconciliationRequest
        {
            NhsNumber = "12345"
        };

        // Act
        var service = new MatchPersonApiService(httpClient);
        var result = await service.ReconcilePersonAsync(validPayload);

        // Assert
        Assert.Equal(validResult.Person.NhsNumber, result?.Person?.NhsNumber);
        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Exactly(1),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }
}