using Shared;
using Shared.Models;

namespace Unit.Tests.Models;

public class SearchSpecificationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("strategy1")]
    public void ShouldAlwaysDefaultValueOnSearchStrategy_WhenNotProvidedInPersonSpecification(string? val)
    {
        // Arrange
        SearchSpecification personSpecification = new()
        {
            SearchStrategy = val!
        };
        
        // Act
        Assert.Equal(SharedConstants.SearchStrategy.Strategies.Strategy1, personSpecification.SearchStrategy);
    }

    [Fact]
    public void ShouldSetValueOnSearchStrategy_WhenProvidedInPersonSpecification()
    {
        // Arrange
        SearchSpecification personSpecification = new()
        {
            SearchStrategy = SharedConstants.SearchStrategy.Strategies.Strategy2
        };
        
        // Act
        Assert.Equal(SharedConstants.SearchStrategy.Strategies.Strategy2, personSpecification.SearchStrategy);
    }
}