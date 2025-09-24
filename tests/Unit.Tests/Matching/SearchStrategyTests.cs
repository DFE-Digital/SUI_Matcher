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
        BirthDate = DateOnly.FromDateTime(new DateTime(2010, 1, 1)),
        Email = "john.doe@example.com"
    };
    
    
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
    public void Strategy1_ReturnsExpectedOrderOfQueries()
    {
        var strat1 = new SearchStrategy1();

        var queries = strat1.BuildQuery(_searchSpecification);
        var expectedOrder = new[]
        {
            "ExactGFD",
            "ExactAll",
            "FuzzyGFD",
            "FuzzyAll",
            "FuzzyGFDRange",
            "FuzzyAltDob"
        };
        Assert.Equal(expectedOrder, queries.Keys.ToArray()) ;
    }

    [Fact]
    public void Strategy2_ReturnsExpectedOrderOfQueries()
    {
        var strat1 = new SearchStrategy2();

        var queries = strat1.BuildQuery(_searchSpecification);
        var expectedOrder = new[]
        {
            "NonFuzzyGFD", "NonFuzzyGFDRange", "NonFuzzyAll", "FuzzyGFD", "FuzzyGFDRangePostcode", "FuzzyAll",
            "FuzzyAltDob"
        };
        
        Assert.Equal(expectedOrder, queries.Keys.ToArray());
    }

    [Fact]
    public void Strategy3_ReturnsExpectedOrderOfQueries()
    {
        var strat1 = new SearchStrategy3();
        
        var queries = strat1.BuildQuery(_searchSpecification);
        var expectedOrder = new[]
        {
            "NonFuzzyGFD", "NonFuzzyGFDRange", "NonFuzzyAll", "FuzzyGFD", "FuzzyGFDRangePostcode", "FuzzyAll", "FuzzyAltDob"
        };
        
        Assert.Equal(expectedOrder, queries.Keys.ToArray());
    }
}