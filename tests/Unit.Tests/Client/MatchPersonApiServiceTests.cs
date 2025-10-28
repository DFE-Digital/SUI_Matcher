using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Moq;
using Moq.Protected;

using Shared;
using Shared.Models;

using SUI.Client.Core.Integration;
using SUI.Client.Core.Models;

namespace Unit.Tests.Client;

public class MatchPersonApiServiceTests
{
    [Fact]
    public async Task MatchPerson_Given_Valid_Request_And_Response_Returns_Correct_Dto()
    {
        // Arrange
        var validResult = new PersonMatchResponse { Result = new MatchResult { MatchStatus = MatchStatus.Match, NhsNumber = "12345" } };
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
        Assert.Equal(validResult.Result.MatchStatus, result?.Result?.MatchStatus);
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
    public async Task MatchPerson_Given_Valid_MatchStatusError_Returns_Correct_Dto()
    {
        // Arrange
        var matchResult = new PersonMatchResponse { Result = new MatchResult { MatchStatus = MatchStatus.Error } };
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
                StatusCode = HttpStatusCode.BadRequest,
                Content = JsonContent.Create(matchResult),
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
        Assert.Equal(matchResult.Result.MatchStatus, result?.Result?.MatchStatus);
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
    public async Task MatchPerson_Given_ReasonPhrase_ThrowsNotSupportedException()
    {
        // Arrange
        var errorMessage = SharedConstants.SearchStrategy.VersionErrorMessagePrefix;
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
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent(errorMessage),
            })
            .Verifiable();

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost:5000/")
        };

        // Act
        var validPayload = new MatchPersonPayload { AddressPostalCode = "LF123ED" };
        var service = new MatchPersonApiService(httpClient);

        // Assert
        var exception = await Assert.ThrowsAsync<NotSupportedException>(
            () => service.MatchPersonAsync(validPayload)
        );
        Assert.Equal(errorMessage, exception.Message);
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
    public async Task ReconcilePerson_Given_Valid_Request_And_Response_Returns_Correct_Dto()
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

    [Fact]
    public async Task ReconcilePerson_Given_Valid_MatchStatusError_Returns_Correct_Dto()
    {
        // Arrange
        var matchResult = new ReconciliationResponse { MatchingResult = new MatchResult { MatchStatus = MatchStatus.Error } };
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
                StatusCode = HttpStatusCode.BadRequest,
                Content = JsonContent.Create(matchResult),
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
        Assert.Equal(matchResult.MatchingResult.MatchStatus, result?.MatchingResult?.MatchStatus);
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