using System.Text.Json;

using MatchingApi;

namespace Unit.Tests.Matching;

[TestClass]
public class CustomDateOnlyConverterTests
{
    [TestMethod]
    public void Read_ReturnsDateOnly_WhenValidDateStringProvided()
    {
        var json = "\"2023-10-01\"";
        var options = new JsonSerializerOptions { Converters = { new CustomDateOnlyConverter() } };

        var result = JsonSerializer.Deserialize<DateOnly?>(json, options);

        Assert.IsNotNull(result);
        Assert.AreEqual(new DateOnly(2023, 10, 1), result);
    }

    [TestMethod]
    public void Read_ReturnsNull_WhenEmptyStringProvided()
    {
        var json = "\"\"";
        var options = new JsonSerializerOptions { Converters = { new CustomDateOnlyConverter() } };

        var result = JsonSerializer.Deserialize<DateOnly?>(json, options);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void Write_SerializesDateOnlyToString_WhenDateOnlyProvided()
    {
        var date = new DateOnly(2023, 10, 1);
        var options = new JsonSerializerOptions { Converters = { new CustomDateOnlyConverter() } };

        var result = JsonSerializer.Serialize(date, options);

        Assert.AreEqual("\"2023-10-01\"", result);
    }

    [TestMethod]
    public void Write_SerializesNullToString_WhenNullProvided()
    {
        DateOnly? date = null;
        var options = new JsonSerializerOptions { Converters = { new CustomDateOnlyConverter() } };

        var result = JsonSerializer.Serialize(date, options);

        Assert.AreEqual("null", result);
    }
}