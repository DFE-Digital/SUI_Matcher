using SUI.Client.Core.Infrastructure.Parsing;

namespace Unit.Tests.Client.ParserTests;

public class AddressDelimiterParserTests
{
    [Fact]
    public void ParseRecord_ParsesHouseFromSecondSegment_AndPostcodeFromLast()
    {
        const string input = "1~2 bob lane~Somewhere~YO1 6GA";

        var result = AddressParser.ParseRecord(input);

        Assert.NotNull(result);
        Assert.Equal("2", result.HouseNumber);
        Assert.Equal("YO16GA", result.Postcode);
    }

    [Theory]
    [InlineData("1~12A Bob Lane~Somewhere~YO1 6GA", "12A", "YO16GA")]
    [InlineData("1~  99  Bob Lane~Somewhere~yo1 6ga", "99", "YO16GA")]
    public void ParseRecord_HandlesSpacingAndCasing(string input, string expectedHouse, string expectedPostcode)
    {
        var result = AddressParser.ParseRecord(input);

        Assert.NotNull(result);
        Assert.Equal(expectedHouse, result!.HouseNumber);
        Assert.Equal(expectedPostcode, result.Postcode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ParseRecord_ReturnsNull_WhenInputBlank(string? input)
    {
        var result = AddressParser.ParseRecord(input);

        Assert.Null(result);
    }

    [Theory]
    [InlineData("1~2 bob lane")] // missing postcode segment
    [InlineData("1~YO1 6GA")] // missing address line segment
    [InlineData("1~bob lane~Somewhere~YO1 6GA")] // no leading house number in segment 1
    public void ParseRecord_ReturnsNull_WhenContractNotMet(string input)
    {
        var result = AddressParser.ParseRecord(input);

        Assert.Null(result);
    }

    [Fact]
    public void ParseRecord_IgnoresTrailingPipe()
    {
        var input = "1~2 bob lane~Somewhere~YO1 6GA";

        var result = AddressParser.ParseRecord(input);

        Assert.NotNull(result);
        Assert.Equal("2", result!.HouseNumber);
        Assert.Equal("YO16GA", result.Postcode);
    }

    [Fact]
    public void ParseHistory_ParsesMultipleRecords_DelimitedByPipe()
    {
        var input = "1~2 bob lane~Somewhere~YO1 6GA|2~3 alice road~Elsewhere~YO2 7GB";

        var result = AddressParser.ParseHistory(input);

        Assert.NotNull(result);
        Assert.Equal(2, result.Addresses.Count);
        Assert.Equal("2", result.Addresses[0].HouseNumber);
        Assert.Equal("YO16GA", result.Addresses[0].Postcode);
        Assert.Equal("3", result.Addresses[1].HouseNumber);
        Assert.Equal("YO27GB", result.Addresses[1].Postcode);
    }

    [Theory]
    [InlineData("1~2 bob lane~Somewhere~YO1 6GA|", 1)] // trailing pipe should be ignored
    [InlineData("|1~2 bob lane~Somewhere~YO1 6GA", 1)] // leading pipe should be ignored
    [InlineData("1~2 bob lane~Somewhere~YO1 6GA||2~3 alice road~Elsewhere~YO2 7GB", 2)] // empty record between pipes should be ignored
    public void ParseHistory_IgnoresEmptyRecords(string input, int expectedCount)
    {
        var result = AddressParser.ParseHistory(input);
        
        Assert.NotNull(result);
        Assert.Equal(expectedCount, result.Addresses.Count);
    }

    [Theory]
    [InlineData("1~2 bob lane~Somewhere~YO1 6GA|2~3 alice road~Elsewhere~YO2 7GB", "3", "YO27GB")]
    [InlineData("|2~alice road~Elsewhere~YO2 7GB|1~2 bob lane~Somewhere~YO1 6GA|2~3~alice road~Elsewhere~YO2 7GB", "3", "YO27GB")]
    public void ParseHistory_PrimaryAddressIsLastEntry(string input, string expectedHouseNumber, string expectedPostcode)
    {
        var result = AddressParser.ParseHistory(input);
        
        Assert.NotNull(result);
        Assert.NotNull(result.PrimaryAddress);
        Assert.Equal(expectedHouseNumber, result.PrimaryAddress!.HouseNumber);
        Assert.Equal(expectedPostcode, result.PrimaryAddress.Postcode);
    }
}