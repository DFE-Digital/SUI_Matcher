using Shared.Models;

namespace Unit.Tests.Shared.Models;

public class MatchResultTests
{
    [Fact]
    public void Should_ReturnTrue_When_StatusIsMatchAndScoreIsAboveThreshold()
    {
        var sut = new MatchResult { MatchStatus = MatchStatus.Match, Score = 0.96m };

        Assert.True(sut.IsHighConfidenceMatch);
    }

    [Fact]
    public void Should_ReturnFalse_When_ScoreIsExactlyThreshold()
    {
        var sut = new MatchResult { MatchStatus = MatchStatus.Match, Score = 0.95m };

        Assert.False(sut.IsHighConfidenceMatch);
    }

    [Fact]
    public void Should_ReturnFalse_When_ScoreIsBelowThreshold()
    {
        var sut = new MatchResult { MatchStatus = MatchStatus.Match, Score = 0.94m };

        Assert.False(sut.IsHighConfidenceMatch);
    }

    [Fact]
    public void Should_ReturnFalse_When_StatusIsNotMatch()
    {
        var sut = new MatchResult { MatchStatus = MatchStatus.PotentialMatch, Score = 0.99m };

        Assert.False(sut.IsHighConfidenceMatch);
    }

    [Fact]
    public void Should_ReturnFalse_When_ScoreIsNull()
    {
        var sut = new MatchResult { MatchStatus = MatchStatus.Match, Score = null };

        Assert.False(sut.IsHighConfidenceMatch);
    }
}
