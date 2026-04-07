using System.Text;
using SUI.StorageProcessFunction.Infrastructure.Csv;

namespace Unit.Tests.StorageProcessFunction;

public class PersonSpecificationCsvParserTests
{
    private readonly PersonSpecificationCsvParser _sut = new();

    [Fact]
    public async Task Should_MapSingleCsvRowToPersonSpecification_When_HeadersMatch()
    {
        await using var content = CreateContentStream(
            """
            GivenName,FamilyName,DOB,Postcode
            Jane,Doe,2012-05-10,SW1A 1AA
            """
        );

        var result = await ParseAllAsync(content);

        var person = Assert.Single(result);
        Assert.Equal("Jane", person.Given);
        Assert.Equal("Doe", person.Family);
        Assert.Equal(new DateOnly(2012, 5, 10), person.BirthDate);
        Assert.NotNull(person.RawBirthDate);
        Assert.Equal(["2012-05-10"], person.RawBirthDate);
        Assert.Equal("SW1A 1AA", person.AddressPostalCode);
    }

    [Fact]
    public async Task Should_MapMultipleRows_When_CsvContainsMultipleRecords()
    {
        await using var content = CreateContentStream(
            """
            GivenName,FamilyName,DOB,Postcode
            Jane,Doe,2012-05-10,SW1A 1AA
            John,Smith,2011/04/09,AB1 2CD
            """
        );

        var result = await ParseAllAsync(content);

        Assert.Equal(2, result.Count);
        Assert.Equal("Jane", result[0].Given);
        Assert.Equal("John", result[1].Given);
        Assert.Equal(new DateOnly(2011, 4, 9), result[1].BirthDate);
    }

    [Fact]
    public async Task Should_AcceptMixedCaseHeaders_When_HeaderCasingVaries()
    {
        await using var content = CreateContentStream(
            """
            givenname,FAMILYNAME,dOb,POSTCODE
            Jane,Doe,20120510,SW1A 1AA
            """
        );

        var result = await ParseAllAsync(content);

        Assert.Single(result);
        Assert.Equal("Jane", result[0].Given);
        Assert.Equal(new DateOnly(2012, 5, 10), result[0].BirthDate);
    }

    [Fact]
    public async Task Should_Throw_When_RequiredHeadersAreMissing()
    {
        await using var content = CreateContentStream(
            """
            GivenName,FamilyName,DOB
            Jane,Doe,2012-05-10
            """
        );

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ParseAllAsync(content)
        );

        Assert.Contains("Postcode", exception.Message);
    }

    [Fact]
    public async Task Should_Throw_When_DobCannotBeParsed()
    {
        await using var content = CreateContentStream(
            """
            GivenName,FamilyName,DOB,Postcode
            Jane,Doe,not-a-date,SW1A 1AA
            """
        );

        await Assert.ThrowsAsync<InvalidOperationException>(() => ParseAllAsync(content));
    }

    [Fact]
    public async Task Should_Throw_When_CsvDoesNotContainAnyRecords()
    {
        await using var content = CreateContentStream(
            """
            GivenName,FamilyName,DOB,Postcode
            """
        );

        await Assert.ThrowsAsync<InvalidOperationException>(() => ParseAllAsync(content));
    }

    private static MemoryStream CreateContentStream(string csv) => new(Encoding.UTF8.GetBytes(csv));

    private async Task<List<Shared.Models.PersonSpecification>> ParseAllAsync(Stream content)
    {
        var results = new List<Shared.Models.PersonSpecification>();
        await foreach (
            var person in _sut.ParseAsync(content, "test-file.csv", CancellationToken.None)
        )
        {
            results.Add(person);
        }

        return results;
    }
}
