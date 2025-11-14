using Shared.Util;

namespace Unit.Tests.Util;

public class CsvUtilsTests
{
    [Fact]
    public void WrapInputForCsv_ReturnsDash_WhenInputIsNull()
    {
        string[]? input = null;

        var result = CsvUtils.WrapInputForCsv(input);

        Assert.Equal("-", result);
    }

    [Fact]
    public void WrapInputForCsv_ReturnsDash_WhenInputIsEmpty()
    {
        string[] input = Array.Empty<string>();

        var result = CsvUtils.WrapInputForCsv(input);

        Assert.Equal("-", result);
    }

    [Fact]

    public void WrapInputForCsv_ReturnsRawInput_WhenNoSpecialCharacters()
    {
        string[] input = ["home~64 Higher Street~Leeds~West Yorkshire~LS123EA|", "billing~54 Medium Street~Leeds~West Yorkshire~LS123EH|"
        ];

        var result = CsvUtils.WrapInputForCsv(input);

        Assert.Equal("home~64 Higher Street~Leeds~West Yorkshire~LS123EA| billing~54 Medium Street~Leeds~West Yorkshire~LS123EH|", result);
    }

    [Fact]

    public void WrapInputForCsv_ReturnsWrappedInput_WhenInputContainsCommas()
    {
        string[] input = ["home~64 Higher,Street~Leeds~West Yorkshire~LS123EA|", "billing~54 Medium Str,eet~Leeds~West Yorkshire~LS123EH|"];

        var result = CsvUtils.WrapInputForCsv(input);

        var expectedResult = "\"home~64 Higher,Street~Leeds~West Yorkshire~LS123EA| billing~54 Medium Str,eet~Leeds~West Yorkshire~LS123EH|\"";

        Assert.Equal(expectedResult, result);
    }

    [Fact]

    public void WrapInputForCsv_ReturnsWrappedInput_WhenInputContainsQuotes()
    {
        string[] input = ["home~64 Higher\" Street~Leeds~West Yorkshire~LS123EA|"];

        var result = CsvUtils.WrapInputForCsv(input);

        var expectedResult = "\"home~64 Higher\"\" Street~Leeds~West Yorkshire~LS123EA|\"";

        Assert.Equal(expectedResult, result);
    }

}