using Shared.Models;
using SUI.Client.Core.Infrastructure.CsvParsers;

namespace Unit.Tests.StorageProcessFunction;

public class CsvPersonSpecParserTests
{
    [Fact]
    public void Should_MapPersonSpecification_When_TypeOneRecordIsValid()
    {
        var sut = new CsvPersonSpecParser("TypeOne");
        var record = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["GivenName"] = "Jane",
            ["FamilyName"] = "Doe",
            ["DOB"] = "2012-05-10",
            ["Postcode"] = "SW1A 1AA",
        };

        var result = sut.Parse(record);

        Assert.Equal("Jane", result.Given);
        Assert.Equal("Doe", result.Family);
        Assert.Equal(new DateOnly(2012, 5, 10), result.BirthDate);
        Assert.NotNull(result.RawBirthDate);
        Assert.Equal(["2012-05-10"], result.RawBirthDate);
        Assert.Equal("SW1A 1AA", result.AddressPostalCode);
    }

    [Fact]
    public void Should_MapOptionalFields_When_OptionalValuesArePresent()
    {
        var sut = new CsvPersonSpecParser("TypeOne");
        var record = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["GivenName"] = "Jane",
            ["FamilyName"] = "Doe",
            ["DOB"] = " 20120510 ",
            ["Postcode"] = "SW1A 1AA",
            ["Email"] = "jane.doe@example.com",
            ["Gender"] = "female",
            ["Phone"] = "07123456789",
        };

        var result = sut.Parse(record);

        Assert.Equal(new DateOnly(2012, 5, 10), result.BirthDate);
        Assert.NotNull(result.RawBirthDate);
        Assert.Equal(["20120510"], result.RawBirthDate);
        Assert.Equal("jane.doe@example.com", result.Email);
        Assert.Equal("female", result.Gender);
        Assert.Equal("07123456789", result.Phone);
    }

    [Fact]
    public void Should_ReturnEmptyValues_When_OptionalFieldsAreMissing()
    {
        var sut = new CsvPersonSpecParser("TypeOne");
        var record = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["GivenName"] = "Jane",
            ["FamilyName"] = "Doe",
            ["DOB"] = "2012-05-10",
        };

        var result = sut.Parse(record);

        Assert.Equal("Jane", result.Given);
        Assert.Equal("Doe", result.Family);
        Assert.Equal(new DateOnly(2012, 5, 10), result.BirthDate);
        Assert.Equal(string.Empty, result.AddressPostalCode);
    }

    [Fact]
    public void Should_SetBirthDateToNull_When_DobCannotBeParsed()
    {
        var sut = new CsvPersonSpecParser("TypeOne");
        var record = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["GivenName"] = "Jane",
            ["FamilyName"] = "Doe",
            ["DOB"] = "not-a-date",
            ["Postcode"] = "SW1A 1AA",
        };

        var result = sut.Parse(record);

        Assert.Null(result.BirthDate);
        Assert.NotNull(result.RawBirthDate);
        Assert.Equal(["not-a-date"], result.RawBirthDate);
    }

    [Fact]
    public void Should_Throw_When_ParserTypeIsUnknown()
    {
        var sut = new CsvPersonSpecParser("InvalidParser");
        var record = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var exception = Assert.Throws<InvalidOperationException>(() => sut.Parse(record));

        Assert.Equal("Unknown parser type: InvalidParser.", exception.Message);
    }
}
