using MatchingApi.Search;

using Shared;
using Shared.Models;

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

    [Fact]
    public void SearchStrategy3_ShouldHaveNoDuplicateKeyExceptions()
    {
        var strategy = new SearchStrategy3();
        var versions = strategy.GetAllAlgorithmVersions();
        var spec = new SearchSpecification
        {
            Given = "John",
            Family = "Doe",
            BirthDate = DateOnly.FromDateTime(new DateTime(2010, 1, 1)),
            AddressPostalCode = "AB12 3CD"
        };

        foreach (var version in versions)
        {
            var exception = Record.Exception(() => strategy.BuildQuery(spec, version));
            Assert.Null(exception);
        }
    }

}