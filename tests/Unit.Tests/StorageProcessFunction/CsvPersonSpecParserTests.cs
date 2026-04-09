using Shared.Models;
using SUI.Client.Core.Infrastructure.CsvParsers;

namespace Unit.Tests.StorageProcessFunction;

public class CsvPersonSpecParserTests
{
    [Fact]
    public void Should_MapPersonSpecification_When_TypeOneRecordIsValid()
    {
        var sut = new CsvPersonSpecParser("TypeOne");
        var headers = CreateHeaders("GivenName", "FamilyName", "DOB", "Postcode");
        var records = new List<Dictionary<string, string>>
        {
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["GivenName"] = "Jane",
                ["FamilyName"] = "Doe",
                ["DOB"] = "2012-05-10",
                ["Postcode"] = "SW1A 1AA",
            },
        };

        var result = sut.Parse(records, headers);

        var person = Assert.Single(result);
        Assert.Equal("Jane", person.Given);
        Assert.Equal("Doe", person.Family);
        Assert.Equal(new DateOnly(2012, 5, 10), person.BirthDate);
        Assert.NotNull(person.RawBirthDate);
        Assert.Equal(["2012-05-10"], person.RawBirthDate);
        Assert.Equal("SW1A 1AA", person.AddressPostalCode);
    }

    [Fact]
    public void Should_MapOptionalFields_When_OptionalValuesArePresent()
    {
        var sut = new CsvPersonSpecParser("TypeOne");
        var headers = CreateHeaders(
            "GivenName",
            "FamilyName",
            "DOB",
            "Postcode",
            "Email",
            "Gender",
            "Phone"
        );
        var records = new List<Dictionary<string, string>>
        {
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["GivenName"] = "Jane",
                ["FamilyName"] = "Doe",
                ["DOB"] = " 20120510 ",
                ["Postcode"] = "SW1A 1AA",
                ["Email"] = "jane.doe@example.com",
                ["Gender"] = "female",
                ["Phone"] = "07123456789",
            },
        };

        var result = sut.Parse(records, headers);

        var person = Assert.Single(result);
        Assert.Equal(new DateOnly(2012, 5, 10), person.BirthDate);
        Assert.NotNull(person.RawBirthDate);
        Assert.Equal(["20120510"], person.RawBirthDate);
        Assert.Equal("jane.doe@example.com", person.Email);
        Assert.Equal("female", person.Gender);
        Assert.Equal("07123456789", person.Phone);
    }

    [Fact]
    public void Should_Throw_When_RequiredHeadersAreMissing()
    {
        var sut = new CsvPersonSpecParser("TypeOne");
        var headers = CreateHeaders("GivenName", "FamilyName", "DOB");
        var records = new List<Dictionary<string, string>>
        {
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["GivenName"] = "Jane",
                ["FamilyName"] = "Doe",
                ["DOB"] = "2012-05-10",
            },
        };

        var exception = Assert.Throws<InvalidOperationException>(() => sut.Parse(records, headers));

        Assert.Contains("Postcode", exception.Message);
    }

    [Fact]
    public void Should_SetBirthDateToNull_When_DobCannotBeParsed()
    {
        var sut = new CsvPersonSpecParser("TypeOne");
        var headers = CreateHeaders("GivenName", "FamilyName", "DOB", "Postcode");
        var records = new List<Dictionary<string, string>>
        {
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["GivenName"] = "Jane",
                ["FamilyName"] = "Doe",
                ["DOB"] = "not-a-date",
                ["Postcode"] = "SW1A 1AA",
            },
        };

        var result = sut.Parse(records, headers);

        var person = Assert.Single(result);
        Assert.Null(person.BirthDate);
        Assert.NotNull(person.RawBirthDate);
        Assert.Equal(["not-a-date"], person.RawBirthDate);
    }

    [Fact]
    public void Should_Throw_When_ParserTypeIsUnknown()
    {
        var sut = new CsvPersonSpecParser("InvalidParser");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            sut.Parse([], CreateHeaders("GivenName", "FamilyName", "DOB", "Postcode"))
        );

        Assert.Equal("Unknown parser type: InvalidParser.", exception.Message);
    }

    private static HashSet<string> CreateHeaders(params string[] headers) =>
        headers.ToHashSet(StringComparer.OrdinalIgnoreCase);
}
