using Microsoft.Extensions.Options;
using SUI.Client.Core.Infrastructure.CsvParsers;

namespace Unit.Tests.Client.CoreTests.InfrastructureTests;

public class CsvRequiredHeadersProviderTests
{
    [Fact]
    public void Should_ReturnRequiredHeaders_When_OptionsAreConfigured()
    {
        var sut = new CsvRequiredHeadersProvider(
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
                        Gender = "Sex",
                        Phone = "Telephone",
                        NhsNumber = "NhsNumber",
                    },
                }
            )
        );

        var result = sut.GetRequiredHeaders();

        Assert.Equal(["PersonId", "Forename", "Surname", "DateOfBirth", "PostCode"], result);
        Assert.DoesNotContain("EmailAddress", result);
        Assert.DoesNotContain("Sex", result);
        Assert.DoesNotContain("Telephone", result);
        Assert.DoesNotContain("NhsNumber", result);
    }
}
