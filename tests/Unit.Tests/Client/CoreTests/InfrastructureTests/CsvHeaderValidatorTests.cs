using SUI.Client.Core.Infrastructure.CsvParsers;

namespace Unit.Tests.Client.CoreTests.InfrastructureTests;

public class CsvHeaderValidatorTests
{
    [Fact]
    public void Should_NotThrow_When_HeadersArePresent()
    {
        var headers = new HashSet<string>(
            ["Id", "GivenName", "FamilyName", "DOB", "Postcode"],
            StringComparer.OrdinalIgnoreCase
        );
        var requiredHeaders = new[] { "Id", "GivenName", "FamilyName", "DOB", "Postcode" };

        CsvHeaderValidator.Validate(headers, requiredHeaders);
    }

    [Fact]
    public void Should_Throw_When_HeadersAreMissing()
    {
        var headers = new HashSet<string>(
            ["GivenName", "FamilyName", "DOB"],
            StringComparer.OrdinalIgnoreCase
        );
        var requiredHeaders = new[] { "Id", "GivenName", "FamilyName", "DOB", "Postcode" };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            CsvHeaderValidator.Validate(headers, requiredHeaders)
        );

        Assert.Contains("Id", exception.Message);
        Assert.Contains("Postcode", exception.Message);
    }

    [Fact]
    public void Should_NotThrow_When_ExtraHeadersArePresent()
    {
        var headers = new HashSet<string>(
            ["Id", "GivenName", "FamilyName", "DOB", "Postcode", "ExtraColumn1", "ExtraColumn2"],
            StringComparer.OrdinalIgnoreCase
        );
        var requiredHeaders = new[] { "Id", "GivenName", "FamilyName", "DOB", "Postcode" };

        CsvHeaderValidator.Validate(headers, requiredHeaders);
    }
}
