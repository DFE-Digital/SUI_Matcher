using System.Text.Json;

using Shared.Util;

namespace Unit.Tests.Util;

public class CustomDateOnlyConverterTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new CustomDateOnlyConverter() }
    };

    [Fact]
    public void Read_ReturnsDateOnly_WhenValidDateStringProvided()
    {
        var json = "\"2023-10-01\"";
        var result = JsonSerializer.Deserialize<DateOnly?>(json, Options);

        Assert.NotNull(result);
        Assert.Equal(new DateOnly(2023, 10, 1), result);
    }

    [Fact]
    public void Read_ReturnsNull_WhenEmptyStringProvided()
    {
        var json = "\"\"";
        var result = JsonSerializer.Deserialize<DateOnly?>(json, Options);

        Assert.Null(result);
    }

    [Fact]
    public void Write_SerializesDateOnlyToString_WhenDateOnlyProvided()
    {
        var date = new DateOnly(2023, 10, 1);
        var result = JsonSerializer.Serialize(date, Options);

        Assert.Equal("\"2023-10-01\"", result);
    }

    [Fact]
    public void Write_SerializesNullToString_WhenNullProvided()
    {
        DateOnly? date = null;
        var result = JsonSerializer.Serialize(date, Options);

        Assert.Equal("null", result);
    }

    [Fact]
    public void ReturnsCorrectDateOnly_WhenValidDateStringProvided()
    {
        var dateString = "20/09/2008";
        var result = dateString.ToDateOnly([Constants.DateFormat, Constants.DateAltFormat, Constants.DateAltFormatBritish]);

        Assert.NotNull(result);
        Assert.Equal(new DateOnly(2008, 09, 20), result);
    }
}