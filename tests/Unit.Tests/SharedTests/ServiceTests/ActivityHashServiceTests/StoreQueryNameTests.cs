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

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void Should_ClearBaggageItem_When_QueryNameIsEmptyOrWhitespace(string queryName)
    {
        var harness = new ActivityHashServiceTestHarness();

        using var activityScope = ActivityHashServiceTestHarness.StartActivity();

        harness.Service.StoreQueryName("Query1");
        Assert.Equal(
            "Query1",
            Activity.Current?.GetBaggageItem(SharedConstants.SearchQuery.LogName)
        );

        harness.Service.StoreQueryName(queryName);
        Assert.Null(Activity.Current?.GetBaggageItem(SharedConstants.SearchQuery.LogName));
    }

    [Fact]
    public void Should_NotThrow_When_ActivityCurrentIsNull()
    {
        var harness = new ActivityHashServiceTestHarness();
        Activity.Current = null;

        var exception = Record.Exception(() => harness.Service.StoreQueryName("Query1"));
        Assert.Null(exception);

        using (var scope = harness.Service.BeginQueryScope("Query1"))
        {
            Assert.NotNull(scope);
        }
    }

    [Fact]
    public void Should_RestoreOuterQueryName_When_QueryScopesAreNested()
    {
        var harness = new ActivityHashServiceTestHarness();

        using var activityScope = ActivityHashServiceTestHarness.StartActivity();

        using (harness.Service.BeginQueryScope("OuterQuery"))
        {
            using (harness.Service.BeginQueryScope("InnerQuery"))
            {
                Assert.Equal(
                    "InnerQuery",
                    Activity.Current?.GetBaggageItem(SharedConstants.SearchQuery.LogName)
                );
            }

            Assert.Equal(
                "OuterQuery",
                Activity.Current?.GetBaggageItem(SharedConstants.SearchQuery.LogName)
            );
        }

        Assert.Null(Activity.Current?.GetBaggageItem(SharedConstants.SearchQuery.LogName));
    }

    [Fact]
    public void Should_RestoreOnOpeningActivity_When_CurrentActivityChangedBeforeDispose()
    {
        var harness = new ActivityHashServiceTestHarness();

        using var outerActivityScope = ActivityHashServiceTestHarness.StartActivity();
        var outerActivity = outerActivityScope.Activity;

        var queryScope = harness.Service.BeginQueryScope("ScopedQuery");
        Assert.Equal(
            "ScopedQuery",
            outerActivity.GetBaggageItem(SharedConstants.SearchQuery.LogName)
        );

        using (ActivityHashServiceTestHarness.StartActivity())
        {
            // Dispose while a different activity is current: the scope must still restore the
            // activity it changed, not whichever one happens to be current now.
            queryScope.Dispose();
        }

        Assert.Null(outerActivity.GetBaggageItem(SharedConstants.SearchQuery.LogName));
    }

    [Fact]
    public void Should_SetAndClearBaggage_When_UsingBeginQueryScope()
    {
        var harness = new ActivityHashServiceTestHarness();

        using var activityScope = ActivityHashServiceTestHarness.StartActivity();

        using (harness.Service.BeginQueryScope("ScopedQuery"))
        {
            Assert.Equal(
                "ScopedQuery",
                Activity.Current?.GetBaggageItem(SharedConstants.SearchQuery.LogName)
            );
        }

        Assert.Null(Activity.Current?.GetBaggageItem(SharedConstants.SearchQuery.LogName));
    }
}
