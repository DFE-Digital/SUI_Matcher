using MatchingApi.Exceptions;
using MatchingApi.Search;

using Shared;
using Shared.Models;

namespace Unit.Tests.Matching;

public class SearchStrategyTests
{
    private readonly SearchSpecification _searchSpecification = new()
    {
        Given = "John",
        Family = "Doe",
        BirthDate = DateOnly.FromDateTime(DateTime.Today),
    };

    [Theory]
    [InlineData(SharedConstants.SearchStrategy.Strategies.Strategy1, typeof(SearchStrategy1))]
    [InlineData(SharedConstants.SearchStrategy.Strategies.Strategy2, typeof(SearchStrategy2))]
    [InlineData(SharedConstants.SearchStrategy.Strategies.Strategy3, typeof(SearchStrategy3))]
    [InlineData(SharedConstants.SearchStrategy.Strategies.Strategy4, typeof(SearchStrategy4))]
    [InlineData(SharedConstants.SearchStrategy.Strategies.Strategy5, typeof(SearchStrategy5))]
    public void SearchStrategyFactory_ReturnsCorrectStrategyInstance(string strategyName, Type expectedType)
    {
        var factory = SearchStrategyFactory.Get(strategyName);
        Assert.IsType(expectedType, factory);
    }

    [Fact]
    public void SearchStrategyFactory_ThrowsOnUnknownStrategy()
    {
        Assert.Throws<ArgumentException>(() => SearchStrategyFactory.Get("unknown"));
    }
    
    [Fact]
    public void SearchStrategy5_ShouldHaveNoDuplicateKeyExceptions()
    {
        var strategy = new SearchStrategy5();
        var versions = strategy.GetAllAlgorithmVersions();

        foreach (var version in versions)
        {
            var sut = new SearchStrategy5(version);
            var exception = Record.Exception(() => sut.BuildQuery(_searchSpecification));
            Assert.Null(exception);
        }
    }

    [Fact]
    public void SearchStrategy4_ShouldHaveNoDuplicateKeyExceptions()
    {
        var strategy = new SearchStrategy4();
        var versions = strategy.GetAllAlgorithmVersions();

        foreach (var version in versions)
        {
            var sut = new SearchStrategy4(version);
            var exception = Record.Exception(() => sut.BuildQuery(_searchSpecification));
            Assert.Null(exception);
        }
    }

    [Fact]
    public void SearchStrategy3_ShouldHaveNoDuplicateKeyExceptions()
    {
        var strategy = new SearchStrategy3();
        var versions = strategy.GetAllAlgorithmVersions();

        foreach (var version in versions)
        {
            var sut = new SearchStrategy3(version);
            var exception = Record.Exception(() => sut.BuildQuery(_searchSpecification));
            Assert.Null(exception);
        }
    }

    [Fact]
    public void SearchStrategy2_ShouldHaveNoDuplicateKeyExceptions()
    {
        var versions = new SearchStrategy2().GetAllAlgorithmVersions();

        foreach (var version in versions)
        {
            var sut = new SearchStrategy3(version);
            var exception = Record.Exception(() => sut.BuildQuery(_searchSpecification));
            Assert.Null(exception);
        }
    }

    [Fact]
    public void SearchStrategy1_ShouldHaveNoDuplicateKeyExceptions()
    {
        var versions = new SearchStrategy1().GetAllAlgorithmVersions();

        foreach (var version in versions)
        {
            var sut = new SearchStrategy1(version);
            var exception = Record.Exception(() => sut.BuildQuery(_searchSpecification));
            Assert.Null(exception);
        }
    }

    [Fact]
    public void SearchStrategy1_ShouldSetCorrectVersion_WhenSettingVersion()
    {
        var strategy = new SearchStrategy1(3);
        Assert.Equal(3, strategy.GetAlgorithmVersion());
    }

    [Fact]
    public void SearchStrategy2_ShouldSetCorrectVersion_WhenSettingVersion()
    {
        var strategy = new SearchStrategy2(1);
        Assert.Equal(1, strategy.GetAlgorithmVersion());
    }

    [Fact]
    public void SearchStrategy3_ShouldSetCorrectVersion_WhenSettingVersion()
    {
        var strategy = new SearchStrategy3(13);
        Assert.Equal(13, strategy.GetAlgorithmVersion());
    }

    [Fact]
    public void SearchStrategy4_ShouldSetCorrectVersion_WhenSettingVersion()
    {
        var strategy = new SearchStrategy4(1);
        Assert.Equal(1, strategy.GetAlgorithmVersion());
    }

    [Fact]
    public void SearchStrategy1_ShouldThrowException_WhenVersionIsOutOfRange()
    {
        Assert.Throws<InvalidStrategyException>(() => new SearchStrategy1(999));
    }

    [Fact]
    public void SearchStrategy2_ShouldThrowException_WhenVersionIsOutOfRange()
    {
        Assert.Throws<InvalidStrategyException>(() => new SearchStrategy2(-1));
    }

    [Fact]
    public void SearchStrategy3_ShouldThrowException_WhenVersionIsOutOfRange()
    {
        Assert.Throws<InvalidStrategyException>(() => new SearchStrategy3(1234));
    }

    [Fact]
    public void SearchStrategy4_ShouldThrowException_WhenVersionIsOutOfRange()
    {
        Assert.Throws<InvalidStrategyException>(() => new SearchStrategy4(1234));
    }
}