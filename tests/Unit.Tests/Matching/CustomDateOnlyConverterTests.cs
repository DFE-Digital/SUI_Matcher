using System.Text.Json;

using MatchingApi;

namespace Unit.Tests.Matching;

public class CustomDateOnlyConverterTests
{
    [Fact]
    public void Read_ReturnsDateOnly_WhenValidDateStringProvided()
    {
        var json = "\"2023-10-01\"";
        var options = new JsonSerializerOptions { Converters = { new CustomDateOnlyConverter() } };

        var result = JsonSerializer.Deserialize<DateOnly?>(json, options);

        Assert.NotNull(result);
        Assert.Equal(new DateOnly(2023, 10, 1), result);
    }

    [Fact]
    public void Read_ReturnsNull_WhenEmptyStringProvided()
    {
        var json = "\"\"";
        var options = new JsonSerializerOptions { Converters = { new CustomDateOnlyConverter() } };

        var result = JsonSerializer.Deserialize<DateOnly?>(json, options);

        Assert.Null(result);
    }

    [Fact]
    public void Write_SerializesDateOnlyToString_WhenDateOnlyProvided()
    {
        var date = new DateOnly(2023, 10, 1);
        var options = new JsonSerializerOptions { Converters = { new CustomDateOnlyConverter() } };

        var result = JsonSerializer.Serialize(date, options);

        Assert.Equal("\"2023-10-01\"", result);
    }

    [Fact]
    public void Write_SerializesNullToString_WhenNullProvided()
    {
        DateOnly? date = null;
        var options = new JsonSerializerOptions { Converters = { new CustomDateOnlyConverter() } };

        var result = JsonSerializer.Serialize(date, options);

        Assert.Equal("null", result);
    }
}