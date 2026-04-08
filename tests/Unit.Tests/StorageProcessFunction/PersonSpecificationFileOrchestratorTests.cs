using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Shared.Models;
using SUI.Client.Core.Application.Interfaces;
using SUI.StorageProcessFunction;
using SUI.StorageProcessFunction.Application;

namespace Unit.Tests.StorageProcessFunction;

public class PersonRecordOrchestratorTests
{
    private readonly Mock<IMatchingApiClient> _matchingApiClient = new();
    private readonly StorageProcessFunctionOptions _options = new()
    {
        SearchStrategy = Shared.SharedConstants.SearchStrategy.Strategies.Strategy4,
        StrategyVersion = 2,
    };

    [Fact]
    public async Task Should_SendParsedRecordToMatchingApi_When_RecordIsValid()
    {
        SearchSpecification? sentPayload = null;
        var content = new List<PersonSpecification>
        {
            CreatePerson("Jane", "Doe", new DateOnly(2012, 5, 10), "SW1A 1AA"),
        };
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

        await sut.ProcessAsync(content, "test-file.csv", CancellationToken.None);

        _matchingApiClient.Verify(
            x => x.MatchPersonAsync(It.IsAny<SearchSpecification>(), CancellationToken.None),
            Times.Once
        );
        Assert.NotNull(sentPayload);
        Assert.Equal("Jane", sentPayload!.Given);
        Assert.Equal("Doe", sentPayload.Family);
        Assert.Equal(new DateOnly(2012, 5, 10), sentPayload.BirthDate);
    }

    [Fact]
    public async Task Should_UseStrategy4Version2_When_SendingRecord()
    {
        SearchSpecification? sentPayload = null;
        var content = new List<PersonSpecification>
        {
            CreatePerson("Jane", "Doe", new DateOnly(2012, 5, 10), "SW1A 1AA"),
        };
        _matchingApiClient
            .Setup(x => x.MatchPersonAsync(It.IsAny<SearchSpecification>(), CancellationToken.None))
            .Callback<SearchSpecification, CancellationToken>((payload, _) => sentPayload = payload)
            .ReturnsAsync(
                new PersonMatchResponse
                {
                    Result = new MatchResult { MatchStatus = MatchStatus.Match },
                }
            );

        await CreateSut().ProcessAsync(content, "test-file.csv", CancellationToken.None);

        Assert.NotNull(sentPayload);
        Assert.Equal(
            Shared.SharedConstants.SearchStrategy.Strategies.Strategy4,
            sentPayload!.SearchStrategy
        );
        Assert.Equal(2, sentPayload.StrategyVersion);
    }

    [Fact]
    public async Task Should_ContinueToNextRecord_When_PreviousMatchFails()
    {
        var content = new List<PersonSpecification>
        {
            CreatePerson("Jane", "Doe", new DateOnly(2012, 5, 10), "SW1A 1AA"),
            CreatePerson("John", "Smith", new DateOnly(2011, 4, 9), "AB1 2CD"),
        };
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
        var sut = CreateSut();

        await sut.ProcessAsync(content, "test-file.csv", CancellationToken.None);

        _matchingApiClient.Verify(
            x => x.MatchPersonAsync(It.IsAny<SearchSpecification>(), CancellationToken.None),
            Times.Exactly(2)
        );
    }

    private PersonRecordOrchestrator CreateSut() =>
        new(
            NullLogger<PersonRecordOrchestrator>.Instance,
            _matchingApiClient.Object,
            Options.Create(_options)
        );

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
