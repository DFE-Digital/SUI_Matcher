using Microsoft.Extensions.Options;
using SUI.Client.Core.Infrastructure.CsvParsers;

namespace Unit.Tests.Client.CoreTests.InfrastructureTests;

public class CsvMatchingHeadersProviderTests
{
    [Fact]
    public void Should_ReturnRequiredHeaders_When_OptionsAreConfigured()
    {
        var sut = new CsvMatchingHeadersProvider(
            Options.Create(
                new CsvMatchDataOptions
                {
                    DateFormat = "yyyy-MM-dd",
                    ColumnMappings = new CsvMatchDataOptions.Headers
                    {
                        Id = "PersonId",
                        Given = "Forename",
                        Family = "Surname",
                        BirthDate = "DateOfBirth",
                        Postcode = "PostCode",
                        Email = "EmailAddress",
                        Gender = "Gender",
                        Phone = "Telephone",
                        NhsNumber = "NhsNumber",
                    },
                }
            )
        );

        var result = sut.GetRequiredHeaders();

        Assert.Equal(["PersonId", "Forename", "Surname", "DateOfBirth", "PostCode"], result);
        Assert.DoesNotContain("EmailAddress", result);
        Assert.DoesNotContain("Gender", result);
        Assert.DoesNotContain("Telephone", result);
        Assert.DoesNotContain("NhsNumber", result);
    }

    [Fact]
    public void Should_ReturnOptionalHeaders_When_OptionsAreConfigured()
    {
        var sut = new CsvMatchingHeadersProvider(
            Options.Create(
                new CsvMatchDataOptions
                {
                    DateFormat = "yyyy-MM-dd",
                    ColumnMappings = new CsvMatchDataOptions.Headers
                    {
                        Id = "PersonId",
                        Given = "Forename",
                        Family = "Surname",
                        BirthDate = "DateOfBirth",
                        Postcode = "PostCode",
                        Email = "EmailAddress",
                        Gender = "Gender",
                        Phone = "Telephone",
                        NhsNumber = "NhsNumber",
                    },
                }
            )
        );

        var result = sut.GetOptionalHeaders();

        Assert.Equal(["EmailAddress", "Gender", "Telephone"], result);
        Assert.DoesNotContain("PersonId", result);
        Assert.DoesNotContain("Forename", result);
        Assert.DoesNotContain("Surname", result);
        Assert.DoesNotContain("DateOfBirth", result);
        Assert.DoesNotContain("PostCode", result);
    }
}
