using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Shared.Models;
using SUI.Client.Core.Infrastructure.Http;
using SUI.StorageProcessFunction;
using SUI.StorageProcessFunction.Application;

namespace Unit.Tests.StorageProcessFunction;

public class BlobPayloadProcessorTests
{
    private readonly Mock<IBlobPersonSpecificationCsvParser> _parser = new();
    private readonly Mock<IMatchingApiRateLimiter> _rateLimiter = new();
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
        var blobFile = CreateBlobFile();
        _parser
            .Setup(x => x.ParseAsync(blobFile, CancellationToken.None))
            .ReturnsAsync([CreatePerson("Jane", "Doe", new DateOnly(2012, 5, 10), "SW1A 1AA")]);
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

        await sut.ProcessAsync(blobFile, CancellationToken.None);

        _parser.Verify(x => x.ParseAsync(blobFile, CancellationToken.None), Times.Once);
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
        var blobFile = CreateBlobFile();
        _parser
            .Setup(x => x.ParseAsync(blobFile, CancellationToken.None))
            .ReturnsAsync([CreatePerson("Jane", "Doe", new DateOnly(2012, 5, 10), "SW1A 1AA")]);
        _matchingApiClient
            .Setup(x => x.MatchPersonAsync(It.IsAny<SearchSpecification>(), CancellationToken.None))
            .Callback<SearchSpecification, CancellationToken>((payload, _) => sentPayload = payload)
            .ReturnsAsync(
                new PersonMatchResponse
                {
                    Result = new MatchResult { MatchStatus = MatchStatus.Match },
                }
            );

        await CreateSut().ProcessAsync(blobFile, CancellationToken.None);

        Assert.NotNull(sentPayload);
        Assert.Equal(
            Shared.SharedConstants.SearchStrategy.Strategies.Strategy4,
            sentPayload!.SearchStrategy
        );
        Assert.Equal(2, sentPayload.StrategyVersion);
    }

    [Fact]
    public async Task Should_ContinueToNextRecord_When_PreviousSendFails()
    {
        var blobFile = CreateBlobFile();
        _parser
            .Setup(x => x.ParseAsync(blobFile, CancellationToken.None))
            .ReturnsAsync([
                CreatePerson("Jane", "Doe", new DateOnly(2012, 5, 10), "SW1A 1AA"),
                CreatePerson("John", "Smith", new DateOnly(2011, 4, 9), "AB1 2CD"),
            ]);
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

        await sut.ProcessAsync(blobFile, CancellationToken.None);

        _matchingApiClient.Verify(
            x => x.MatchPersonAsync(It.IsAny<SearchSpecification>(), CancellationToken.None),
            Times.Exactly(2)
        );
    }

    private BlobPayloadProcessor CreateSut() =>
        new(
            NullLogger<BlobPayloadProcessor>.Instance,
            _parser.Object,
            _matchingApiClient.Object,
            Options.Create(_options)
        );

    private static BlobFileContent CreateBlobFile() =>
        new(
            new StorageBlobMessage { ContainerName = "incoming", BlobName = "test-file.csv" },
            BinaryData.FromString("ignored"),
            "text/csv"
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
