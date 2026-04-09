using SUI.Client.Core.Infrastructure.CsvParsers;

namespace Unit.Tests.StorageProcessFunction;

public class CsvHeaderValidatorTests
{
    [Fact]
    public void Should_NotThrow_When_HeadersArePresent()
    {
        var headers = new HashSet<string>(
            ["GivenName", "FamilyName", "DOB", "Postcode"],
            StringComparer.OrdinalIgnoreCase
        );

        CsvHeaderValidator.Validate(headers, CsvParserNameConstants.TypeOne);
    }

    [Fact]
    public void Should_Throw_When_HeadersAreMissing()
    {
        var headers = new HashSet<string>(
            ["GivenName", "FamilyName", "DOB"],
            StringComparer.OrdinalIgnoreCase
        );

        var exception = Assert.Throws<InvalidOperationException>(() =>
            CsvHeaderValidator.Validate(headers, CsvParserNameConstants.TypeOne)
        );

        Assert.Contains("Postcode", exception.Message);
    }

    [Fact]
    public void Should_Throw_When_ParserTypeIsUnknown()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            CsvHeaderValidator.Validate([], "InvalidParser")
        );

        Assert.Equal("Unknown parser type: InvalidParser.", exception.Message);
    }
}
