using System.Diagnostics;
using Shared;

namespace Unit.Tests.SharedTests.ServiceTests.ActivityHashServiceTests;

public class GetUniqueSearchIdTests
{
    [Fact]
    public void Should_ReturnUniqueSearchIdInCurrentActivity_When_SearchIdHasBeenStored()
    {
        var harness = new ActivityHashServiceTestHarness();
        var personSpec = ActivityHashServiceTestHarness.CreatePersonSpecification(
            given: "Jane",
            family: "Smith",
            gender: "female",
            birthDate: new DateOnly(2004, 5, 15),
            addressPostalCode: "XY9 8ZW"
        );

        using var activityScope = ActivityHashServiceTestHarness.StartActivity();

        var hash = harness.Service.StoreUniqueSearchIdFor(personSpec);
        var retrievedHash = harness.Service.GetUniqueSearchId();

        Assert.Equal(hash, retrievedHash);
        Assert.Equal(
            "2f383866c432df9a75556ed5d732bb8e348b134b8bfc5c5394990af77090c155",
            retrievedHash
        );
    }

    [Fact]
    public void Should_StoreAlgorithmVersionInCurrentActivity_When_AlgorithmVersionHasBeenStored()
    {
        var harness = new ActivityHashServiceTestHarness();

        using var activityScope = ActivityHashServiceTestHarness.StartActivity();

        harness.Service.StoreAlgorithmVersion(3);

        Assert.Equal("3", Activity.Current?.GetBaggageItem("AlgorithmVersion"));
    }

    [Fact]
    public void Should_StoreSearchStrategyInCurrentActivity_When_SearchStrategyHasBeenStored()
    {
        var harness = new ActivityHashServiceTestHarness();

        using var activityScope = ActivityHashServiceTestHarness.StartActivity();

        harness.Service.StoreSearchStrategy(SharedConstants.SearchStrategy.Strategies.Strategy3);

        Assert.Equal(
            SharedConstants.SearchStrategy.Strategies.Strategy3,
            Activity.Current?.GetBaggageItem(SharedConstants.SearchStrategy.LogName)
        );
    }
}
