using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shared.Models;
using SUI.Client.Core.Application.UseCases.MatchPeople;
using SUI.Client.Core.Infrastructure.Http;

namespace Unit.Tests.Client;

public class MatchPeopleBatchProcessorTests
{
    private readonly Mock<IMatchingApiRateLimiter> _rateLimiter = new();
    private readonly Mock<IMatchingApiClient> _matchingApiClient = new();

    [Fact]
    public async Task Should_SendEachPersonToMatchingApi_When_RecordIsValid()
    {
        SearchSpecification? sentPayload = null;
        _matchingApiClient
            .Setup(x => x.MatchPersonAsync(It.IsAny<SearchSpecification>(), CancellationToken.None))
            .Callback<SearchSpecification, CancellationToken>((payload, _) => sentPayload = payload)
            .ReturnsAsync(
                new PersonMatchResponse
                {
                    Result = new MatchResult { MatchStatus = MatchStatus.Match },
                }
            );

        var sut = CreateSut();

        var stats = await sut.ProcessAsync(
            new ProcessPersonBatchRequest
            {
                People = [CreatePerson("Jane", "Doe", new DateOnly(2012, 5, 10), "SW1A 1AA")],
                SearchStrategy = "strategy4",
                StrategyVersion = 2,
                BatchIdentifier = "batch-1",
            },
            CancellationToken.None
        );

        Assert.NotNull(sentPayload);
        Assert.Equal("Jane", sentPayload!.Given);
        Assert.Equal("Doe", sentPayload.Family);
        Assert.Equal(new DateOnly(2012, 5, 10), sentPayload.BirthDate);
        Assert.Equal(1, stats.Count);
        Assert.Equal(1, stats.CountMatched);
    }

    [Fact]
    public async Task Should_PreserveSearchConfiguration_When_SendingRecord()
    {
        SearchSpecification? sentPayload = null;
        _matchingApiClient
            .Setup(x => x.MatchPersonAsync(It.IsAny<SearchSpecification>(), CancellationToken.None))
            .Callback<SearchSpecification, CancellationToken>((payload, _) => sentPayload = payload)
            .ReturnsAsync(
                new PersonMatchResponse
                {
                    Result = new MatchResult { MatchStatus = MatchStatus.Match },
                }
            );

        await CreateSut()
            .ProcessAsync(
                new ProcessPersonBatchRequest
                {
                    People = [CreatePerson("Jane", "Doe", new DateOnly(2012, 5, 10), "SW1A 1AA")],
                    SearchStrategy = "strategy4",
                    StrategyVersion = 2,
                },
                CancellationToken.None
            );

        Assert.NotNull(sentPayload);
        Assert.Equal("strategy4", sentPayload!.SearchStrategy);
        Assert.Equal(2, sentPayload.StrategyVersion);
    }

    [Fact]
    public async Task Should_ContinueToNextRecord_When_PreviousSendFails()
    {
        _matchingApiClient
            .SetupSequence(x =>
                x.MatchPersonAsync(It.IsAny<SearchSpecification>(), CancellationToken.None)
            )
            .ThrowsAsync(new HttpRequestException("boom"))
            .ReturnsAsync(
                new PersonMatchResponse
                {
                    Result = new MatchResult { MatchStatus = MatchStatus.Match },
                }
            );

        var stats = await CreateSut()
            .ProcessAsync(
                new ProcessPersonBatchRequest
                {
                    People =
                    [
                        CreatePerson("Jane", "Doe", new DateOnly(2012, 5, 10), "SW1A 1AA"),
                        CreatePerson("John", "Smith", new DateOnly(2011, 4, 9), "AB1 2CD"),
                    ],
                    SearchStrategy = "strategy4",
                    StrategyVersion = 2,
                    BatchIdentifier = "batch-1",
                },
                CancellationToken.None
            );

        _matchingApiClient.Verify(
            x => x.MatchPersonAsync(It.IsAny<SearchSpecification>(), CancellationToken.None),
            Times.Exactly(2)
        );
        Assert.Equal(2, stats.Count);
        Assert.Equal(1, stats.ErroredCount);
        Assert.Equal(1, stats.CountMatched);
    }

    private MatchPeopleBatchProcessor CreateSut() =>
        new(NullLogger<MatchPeopleBatchProcessor>.Instance, _matchingApiClient.Object);

    private static PersonSpecification CreatePerson(
        string given,
        string family,
        DateOnly birthDate,
        string postcode
    ) =>
        new()
        {
            Given = given,
            Family = family,
            BirthDate = birthDate,
            RawBirthDate = [birthDate.ToString("yyyy-MM-dd")],
            AddressPostalCode = postcode,
        };
}
