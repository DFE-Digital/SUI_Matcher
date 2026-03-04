using SUI.Client.Core.Services;

namespace Unit.Tests.Client.AddressComparisonServiceTests;

public class AddressesHistoryIntersectTests
{
    [Theory]
    [InlineData("1~2a bob lane~Somewhere~YO1 6GA", "1~2a bob lane~Somewhere~YO1 6GA", true)]
    [InlineData("1~2 bob lane~Somewhere~YO1 6GA|1~3 bob lane~Somewhere~YO1 6GB",
        "house~3 bob lane~Somewhere~YO1 6GB|temp~15b bob lane~Somewhere else~MA1 6YO",
        true)]
    [InlineData("1~2 bob lane~Somewhere~YO1 6GA", "1~2 bob lane~Somewhere~YO1 6GB", false)]
    [InlineData("1~2 bob lane~Somewhere~YO1 6GA", "house~3 bob lane~Somewhere~YO1 6GA", false)]
    [InlineData("1~2 bob lane~Somewhere~YO1 6GA|1~3 bob lane~Somewhere~YO1 6GB", "house~3 bob lane~Somewhere~YO1 6GA",
        false)]
    [InlineData("1~2 bob lane~Somewhere~YO1 6GA|1~3 bob lane~Somewhere~YO1 6GB", "house~4 bob lane~Somewhere~YO1 6GA",
        false)]
    public void AddressesHistoryIntersect_ReturnsExpectedResult(string address1, string address2, bool expected)
    {
        var result = AddressComparisonService.AddressesHistoryIntersect(address1, address2);
        Assert.Equal(expected, result);
    }
}