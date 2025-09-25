using MatchingApi.Search;

using Shared;

namespace Unit.Tests.Matching;

public class SearchStrategyTests
{
    [Theory]
    [InlineData(SharedConstants.SearchStrategy.Strategies.Strategy1, typeof(SearchStrategy1))]
    [InlineData(SharedConstants.SearchStrategy.Strategies.Strategy2, typeof(SearchStrategy2))]
    [InlineData(SharedConstants.SearchStrategy.Strategies.Strategy3, typeof(SearchStrategy3))]
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
}