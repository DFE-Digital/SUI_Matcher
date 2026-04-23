using Microsoft.Extensions.Options;
using Shared.Models;
using SUI.Client.Core.Application.Models;
using SUI.Client.Core.Infrastructure.CsvParsers;

namespace Unit.Tests.Client.ParserTests;

public class CsvPersonSpecParserTests
{
    private static IOptions<CsvMatchDataOptions> CreateDefaultOptions()
    {
        return Options.Create(
            new CsvMatchDataOptions
            {
                DateFormat = "yyyy-MM-dd",
                ColumnMappings = new CsvMatchDataOptions.Headers
                {
                    Id = "Id",
                    Given = "GivenName",
                    Family = "FamilyName",
                    BirthDate = "DOB",
                    Email = "Email",
                    Postcode = "Postcode",
                    Gender = "Gender",
                    Phone = "Phone",
                    NhsNumber = "NhsNumber",
                },
            }
        );
    }

    [Fact]
    public void Should_MapPersonSpecification_When_RecordIsValid()
    {
        var options = CreateDefaultOptions();
        var sut = new CsvPersonSpecParser(options);
        var record = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["GivenName"] = "Jane",
            ["FamilyName"] = "Doe",
            ["DOB"] = "2012-05-10",
            ["Postcode"] = "SW1A 1AA",
        };
        var csvRecord = new CsvRecordDto(record);

        var result = sut.Parse(csvRecord);

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
        var options = CreateDefaultOptions();
        var sut = new CsvPersonSpecParser(options);
        var record = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["GivenName"] = "Jane",
            ["FamilyName"] = "Doe",
            ["DOB"] = "2012-05-10",
            ["Postcode"] = "SW1A 1AA",
            ["Email"] = "jane.doe@example.com",
            ["Gender"] = "female",
            ["Phone"] = "07123456789",
        };
        var csvRecord = new CsvRecordDto(record);

        var result = sut.Parse(csvRecord);

        Assert.Equal(new DateOnly(2012, 5, 10), result.BirthDate);
        Assert.NotNull(result.RawBirthDate);
        Assert.Equal(["2012-05-10"], result.RawBirthDate);
        Assert.Equal("jane.doe@example.com", result.Email);
        Assert.Equal("female", result.Gender);
        Assert.Equal("07123456789", result.Phone);
    }

    [Fact]
    public void Should_ReturnEmptyValues_When_OptionalFieldsAreMissing()
    {
        var options = CreateDefaultOptions();
        var sut = new CsvPersonSpecParser(options);
        var record = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["GivenName"] = "Jane",
            ["FamilyName"] = "Doe",
            ["DOB"] = "2012-05-10",
        };
        var csvRecord = new CsvRecordDto(record);

        var result = sut.Parse(csvRecord);

        Assert.Equal("Jane", result.Given);
        Assert.Equal("Doe", result.Family);
        Assert.Equal(new DateOnly(2012, 5, 10), result.BirthDate);
        Assert.Equal(string.Empty, result.AddressPostalCode);
    }

    [Fact]
    public void Should_SetBirthDateToNull_When_DobCannotBeParsed()
    {
        var options = CreateDefaultOptions();
        var sut = new CsvPersonSpecParser(options);
        var record = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["GivenName"] = "Jane",
            ["FamilyName"] = "Doe",
            ["DOB"] = "not-a-date",
            ["Postcode"] = "SW1A 1AA",
        };
        var csvRecord = new CsvRecordDto(record);

        var result = sut.Parse(csvRecord);

        Assert.Null(result.BirthDate);
        Assert.NotNull(result.RawBirthDate);
        Assert.Equal(["not-a-date"], result.RawBirthDate);
    }
}
