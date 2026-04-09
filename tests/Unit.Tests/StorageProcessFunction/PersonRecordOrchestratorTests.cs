using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Shared.Models;
using SUI.Client.Core.Application.Interfaces;
using SUI.Client.Core.Application.UseCases.MatchPeople;

namespace Unit.Tests.StorageProcessFunction;

public class PersonRecordOrchestratorTests
{
    private readonly Mock<IMatchingApiClient> _matchingApiClient = new();
    private readonly Mock<IPersonSpecParser<PersonSpecification>> _personSpecParser = new();
    private readonly PersonMatchingOptions _options = new()
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
        _personSpecParser.Setup(x => x.Parse(It.IsAny<PersonSpecification>())).Returns<PersonSpecification>(x => x);
        var sut = CreateSut();

        var result = await sut.ProcessAsync(content, "test-file.csv", CancellationToken.None);

        _matchingApiClient.Verify(
            x => x.MatchPersonAsync(It.IsAny<SearchSpecification>(), CancellationToken.None),
            Times.Once
        );
        Assert.NotNull(sentPayload);
        Assert.Equal("Jane", sentPayload!.Given);
        Assert.Equal("Doe", sentPayload.Family);
        Assert.Equal(new DateOnly(2012, 5, 10), sentPayload.BirthDate);
        var processedRecord = Assert.Single(result);
        Assert.Same(content[0], processedRecord.OriginalData);
        Assert.NotNull(processedRecord.ApiResult);
        Assert.True(processedRecord.IsSuccess);
        Assert.Equal(string.Empty, processedRecord.ErrorMessage);
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
        _personSpecParser.Setup(x => x.Parse(It.IsAny<PersonSpecification>())).Returns<PersonSpecification>(x => x);

        var result = await CreateSut().ProcessAsync(content, "test-file.csv", CancellationToken.None);

        Assert.NotNull(sentPayload);
        Assert.Equal(
            Shared.SharedConstants.SearchStrategy.Strategies.Strategy4,
            sentPayload!.SearchStrategy
        );
        Assert.Equal(2, sentPayload.StrategyVersion);
        Assert.Single(result);
    }

    [Fact]
    public async Task Should_ReturnProcessedRecordWithError_When_RecordFails()
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
            .ThrowsAsync(new HttpRequestException("failure"))
            .ReturnsAsync(
                new PersonMatchResponse
                {
                    Result = new MatchResult { MatchStatus = MatchStatus.Match },
                }
            );
        _personSpecParser.Setup(x => x.Parse(It.IsAny<PersonSpecification>())).Returns<PersonSpecification>(x => x);
        var sut = CreateSut();

        var result = await sut.ProcessAsync(content, "test-file.csv", CancellationToken.None);

        _matchingApiClient.Verify(
            x => x.MatchPersonAsync(It.IsAny<SearchSpecification>(), CancellationToken.None),
            Times.Exactly(2)
        );
        Assert.Equal(2, result.Count);

        var failedRecord = result[0];
        Assert.Same(content[0], failedRecord.OriginalData);
        Assert.Null(failedRecord.ApiResult);
        Assert.False(failedRecord.IsSuccess);
        Assert.Equal("failure", failedRecord.ErrorMessage);

        var successfulRecord = result[1];
        Assert.Same(content[1], successfulRecord.OriginalData);
        Assert.NotNull(successfulRecord.ApiResult);
        Assert.True(successfulRecord.IsSuccess);
        Assert.Equal(string.Empty, successfulRecord.ErrorMessage);
    }

    private PersonRecordOrchestrator<PersonSpecification> CreateSut() =>
        new(
            NullLogger<PersonRecordOrchestrator<PersonSpecification>>.Instance,
            _matchingApiClient.Object,
            _personSpecParser.Object,
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
