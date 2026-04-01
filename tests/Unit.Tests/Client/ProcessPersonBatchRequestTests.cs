using Shared.Models;
using SUI.Client.Core.Application.UseCases.MatchPeople;

namespace Unit.Tests.Client;

public class ProcessPersonBatchRequestTests
{
    [Fact]
    public void Should_PreservePeople_When_RequestIsCreated()
    {
        IReadOnlyList<PersonSpecification> people =
        [
            new PersonSpecification
            {
                Given = "Jane",
                Family = "Doe",
                BirthDate = new DateOnly(2012, 5, 10),
            },
        ];

        var result = new ProcessPersonBatchRequest
        {
            People = people,
            SearchStrategy = "strategy4",
            StrategyVersion = 2,
        };

        Assert.Same(people, result.People);
    }

    [Fact]
    public void Should_PreserveSearchConfiguration_When_RequestIsCreated()
    {
        var result = new ProcessPersonBatchRequest
        {
            People = [],
            SearchStrategy = "strategy4",
            StrategyVersion = 2,
        };

        Assert.Equal("strategy4", result.SearchStrategy);
        Assert.Equal(2, result.StrategyVersion);
    }

    [Fact]
    public void Should_AllowNullBatchIdentifier_When_RequestIsCreated()
    {
        var result = new ProcessPersonBatchRequest
        {
            People = [],
            SearchStrategy = "strategy4",
            BatchIdentifier = null,
        };

        Assert.Null(result.BatchIdentifier);
    }

    [Fact]
    public void Should_PreserveBatchIdentifier_When_RequestIsCreated()
    {
        var result = new ProcessPersonBatchRequest
        {
            People = [],
            SearchStrategy = "strategy4",
            BatchIdentifier = "blob.csv",
        };

        Assert.Equal("blob.csv", result.BatchIdentifier);
    }
}
