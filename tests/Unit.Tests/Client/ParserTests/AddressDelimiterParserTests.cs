using Shared.Models;

using SUI.Client.Core.Infrastructure.Parsing;

namespace Unit.Tests.Client.ParserTests;

public class AddressDelimiterParserTests
{
    [Fact]
    public void ParseRecord_ParsesHouseFromSecondSegment_AndPostcodeFromLast()
    {
        const string historyString = "1~2 bob lane~Somewhere~YO1 6GA";

        var result = AddressParser.ParseRecord(historyString);

        Assert.NotNull(result);
        Assert.Equal("2", result.AddressLineOne);
        Assert.Equal("YO16GA", result.Postcode);
    }

    [Theory]
    [InlineData("1~12A Bob Lane~Somewhere~YO1 6GA", "12A", "YO16GA")]
    [InlineData("1~  99  Bob Lane~Somewhere~yo1 6ga", "99", "YO16GA")]
    public void ParseRecord_HandlesSpacingAndCasing(string historyString, string expectedHouse, string expectedPostcode)
    {
        var result = AddressParser.ParseRecord(historyString);

        Assert.NotNull(result);
        Assert.Equal(expectedHouse, result!.AddressLineOne);
        Assert.Equal(expectedPostcode, result.Postcode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ParseRecord_ReturnsNull_WhenInputBlank(string? historyString)
    {
        var result = AddressParser.ParseRecord(historyString);

        Assert.Null(result);
    }

    [Theory]
    [InlineData("1~2 bob lane")] // missing postcode segment
    [InlineData("1~YO1 6GA")] // missing address line segment
    [InlineData("1~bob lane~Somewhere~YO1 6GA")] // no leading house number in segment 1
    public void ParseRecord_ReturnsNull_WhenContractNotMet(string historyString)
    {
        var result = AddressParser.ParseRecord(historyString);

        Assert.Null(result?.AddressLineOne);
        Assert.Null(result?.AddressLineTwo);
    }

    [Theory]
    [InlineData("1~2~bob lane~York~YO1 6GA", "2", null)]
    [InlineData("1~2-4~bob lane~York~YO1 6GA", "2-4", null)]
    public void ParseRecord_ReturnsCorrectDataInAddressLines(string historyString, string expectedLineOne, string? expectedLineTwo)
    {
        var result = AddressParser.ParseRecord(historyString);

        Assert.Equal(expectedLineOne, result?.AddressLineOne);
        Assert.Equal(expectedLineTwo, result?.AddressLineTwo);
    }

    [Fact]
    public void ParseRecord_IgnoresTrailingPipe()
    {
        var historyString = "1~2 bob lane~Somewhere~YO1 6GA";

        var result = AddressParser.ParseRecord(historyString);

        Assert.NotNull(result);
        Assert.Equal("2", result!.AddressLineOne);
        Assert.Equal("YO16GA", result.Postcode);
    }

    [Fact]
    public void ParseHistory_ParsesMultipleRecords_DelimitedByPipe()
    {
        const string primaryPostcode = "YO2 7GB";
        const string historyString = "1~2 bob lane~Somewhere~YO1 6GA|2~3 alice road~Elsewhere~YO2 7GB";

        var result = AddressParser.ParseHistory(historyString, primaryPostcode);

        Assert.NotNull(result);
        Assert.Equal(2, result.Addresses.Count);
        Assert.Equal("2", result.Addresses[0].AddressLineOne);
        Assert.Equal("YO16GA", result.Addresses[0].Postcode);
        Assert.Equal("3", result.Addresses[1].AddressLineOne);
        Assert.Equal("YO27GB", result.Addresses[1].Postcode);
    }

    [Theory]
    [InlineData("YO1 6GA", "1~2 bob lane~Somewhere~YO1 6GA|", 1)] // trailing pipe should be ignored
    [InlineData("YO1 6GA", "|1~2 bob lane~Somewhere~YO1 6GA", 1)] // leading pipe should be ignored
    [InlineData("YO1 6GA", "1~2 bob lane~Somewhere~YO1 6GA||2~3 alice road~Elsewhere~YO2 7GB", 2)] // empty record between pipes should be ignored
    public void ParseHistory_IgnoresEmptyRecords(string primaryPostcode, string historyString, int expectedCount)
    {
        var result = AddressParser.ParseHistory(historyString, primaryPostcode);

        Assert.NotNull(result);
        Assert.Equal(expectedCount, result.Addresses.Count);
    }

    [Theory]
    [InlineData("YO27GB", "1~2 bob lane~Somewhere~YO1 6GA|2~3 alice road~Elsewhere~YO2 7GB", "3", "YO27GB")]
    [InlineData("YO27GB", "|2~alice road~Elsewhere~YO2 7GB|1~2 bob lane~Somewhere~YO1 6GA|2~3~alice road~Elsewhere~YO2 7GB", "3", "YO27GB")]
    public void ParseHistory_PrimaryAddressIsLastEntryWithMatchingPostcode(string primaryPostcode, string historyString, string expectedHouseNumber, string expectedPostcode)
    {
        var result = AddressParser.ParseHistory(historyString, primaryPostcode);

        Assert.NotNull(result);
        Assert.NotNull(result.PrimaryAddress);
        Assert.Equal(expectedHouseNumber, result.PrimaryAddress!.AddressLineOne);
        Assert.Equal(expectedPostcode, result.PrimaryAddress.Postcode);
    }

    [Fact]
    public void FromNhsPerson_PrimaryAddressIsLastEntryWithMatchingPostcode()
    {
        // Arrange
        const string primaryPostcode = "YO2 7GB";
        string[] history =
        [
            "1~2 bob lane~Somewhere~YO1 6GA",
            "2~3 alice road~Elsewhere~YO2 7GB"
        ];
        var nhsPerson = new NhsPerson { AddressPostalCodes = [primaryPostcode], AddressHistory = history, NhsNumber = "1234567890" };

        var result = AddressParser.FromNhsPerson(nhsPerson);

        Assert.NotNull(result);
        Assert.NotNull(result.PrimaryAddress);
        Assert.Equal("3", result.PrimaryAddress!.AddressLineOne);
        Assert.Equal("YO27GB", result.PrimaryAddress.Postcode);
    }
}