using Shared.Models;
using SUI.Client.Core.Application.UseCases.MatchPeople;

namespace Unit.Tests.StorageProcessFunction;

public class MatchingProcessStatsTests
{
    [Fact]
    public void Should_RecordError_When_ResponseIsNull()
    {
        var sut = new MatchingProcessStats();

        sut.RecordStats(null);

        Assert.Equal(1, sut.Count);
        Assert.Equal(1, sut.ErroredCount);
    }

    [Fact]
    public void Should_RecordMatchedCount_When_ResponseIsMatched()
    {
        var sut = new MatchingProcessStats();

        sut.RecordStats(
            new PersonMatchResponse { Result = new MatchResult { MatchStatus = MatchStatus.Match } }
        );

        Assert.Equal(1, sut.Count);
        Assert.Equal(1, sut.CountMatched);
        Assert.Equal(0, sut.ErroredCount);
    }

    [Fact]
    public void Should_RecordError_When_RecordErrorIsCalled()
    {
        var sut = new MatchingProcessStats();

        sut.RecordError();

        Assert.Equal(1, sut.Count);
        Assert.Equal(1, sut.ErroredCount);
    }

    [Fact]
    public void Should_AccumulateCounts_When_MultipleDifferentStatusesAreRecorded()
    {
        var sut = new MatchingProcessStats();

        sut.RecordStats(
            new PersonMatchResponse { Result = new MatchResult { MatchStatus = MatchStatus.Match } }
        );
        sut.RecordStats(
            new PersonMatchResponse
            {
                Result = new MatchResult { MatchStatus = MatchStatus.PotentialMatch },
            }
        );
        sut.RecordStats(
            new PersonMatchResponse
            {
                Result = new MatchResult { MatchStatus = MatchStatus.LowConfidenceMatch },
            }
        );
        sut.RecordStats(
            new PersonMatchResponse
            {
                Result = new MatchResult { MatchStatus = MatchStatus.ManyMatch },
            }
        );
        sut.RecordStats(
            new PersonMatchResponse
            {
                Result = new MatchResult { MatchStatus = MatchStatus.NoMatch },
            }
        );
        sut.RecordStats(
            new PersonMatchResponse { Result = new MatchResult { MatchStatus = MatchStatus.Error } }
        );

        Assert.Equal(6, sut.Count);
        Assert.Equal(1, sut.CountMatched);
        Assert.Equal(1, sut.CountPotentialMatch);
        Assert.Equal(1, sut.CountLowConfidenceMatch);
        Assert.Equal(1, sut.CountManyMatch);
        Assert.Equal(1, sut.CountNoMatch);
        Assert.Equal(1, sut.ErroredCount);
    }
}
