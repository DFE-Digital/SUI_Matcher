using Microsoft.Extensions.Configuration;
using SUI.Client.Core.Application.Models;

namespace Unit.Tests.Client.CoreTests.ApplicationTests.ModelsTests;

public class OptionalPropertiesLogTests
{
    [Fact]
    public void Should_BindFields_When_ConfiguredAsArray()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [$"{OptionalPropertiesLog.SectionName}:Fields:0"] = "free_school_meals",
                    [$"{OptionalPropertiesLog.SectionName}:Fields:1"] = "AnotherRandomField",
                }
            )
            .Build();

        var options = configuration
            .GetSection(OptionalPropertiesLog.SectionName)
            .Get<OptionalPropertiesLog>();

        Assert.NotNull(options);
        Assert.Equal(["free_school_meals", "AnotherRandomField"], options.Fields);
    }
}
