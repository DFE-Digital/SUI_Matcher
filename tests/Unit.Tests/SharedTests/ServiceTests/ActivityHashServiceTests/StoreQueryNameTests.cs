using System.Diagnostics;
using Shared;

namespace Unit.Tests.SharedTests.ServiceTests.ActivityHashServiceTests;

public class StoreQueryNameTests
{
    [Fact]
    public void Should_SetBaggageItem_When_ActivityIsPresent()
    {
        var harness = new ActivityHashServiceTestHarness();

        using var activityScope = ActivityHashServiceTestHarness.StartActivity();

        harness.Service.StoreQueryName("Query1");

        Assert.Equal(
            "Query1",
            Activity.Current?.GetBaggageItem(SharedConstants.SearchQuery.LogName)
        );
    }

    [Fact]
    public void Should_ClearBaggageItem_When_QueryNameIsNull()
    {
        var harness = new ActivityHashServiceTestHarness();

        using var activityScope = ActivityHashServiceTestHarness.StartActivity();

        harness.Service.StoreQueryName("Query1");
        Assert.Equal(
            "Query1",
            Activity.Current?.GetBaggageItem(SharedConstants.SearchQuery.LogName)
        );

        harness.Service.StoreQueryName(null);
        Assert.Null(Activity.Current?.GetBaggageItem(SharedConstants.SearchQuery.LogName));
    }
}
