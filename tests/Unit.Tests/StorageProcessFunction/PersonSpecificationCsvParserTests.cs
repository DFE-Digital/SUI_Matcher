using SUI.StorageProcessFunction.Infrastructure.Csv;

namespace Unit.Tests.StorageProcessFunction;

public class PersonSpecificationCsvParserTests
{
    private readonly PersonSpecificationCsvParser _sut = new();

    [Fact]
    public async Task Should_MapSingleCsvRowToPersonSpecification_When_HeadersMatch()
    {
        var content = CreateContent(
            """
            GivenName,FamilyName,DOB,Postcode
            Jane,Doe,2012-05-10,SW1A 1AA
            """
        );

        var result = ParseAll(content);

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
        var content = CreateContent(
            """
            GivenName,FamilyName,DOB,Postcode
            Jane,Doe,2012-05-10,SW1A 1AA
            John,Smith,2011/04/09,AB1 2CD
            """
        );

        var result = ParseAll(content);

        Assert.Equal(2, result.Count);
        Assert.Equal("Jane", result[0].Given);
        Assert.Equal("John", result[1].Given);
        Assert.Equal(new DateOnly(2011, 4, 9), result[1].BirthDate);
    }

    [Fact]
    public async Task Should_AcceptMixedCaseHeaders_When_HeaderCasingVaries()
    {
        var content = CreateContent(
            """
            givenname,FAMILYNAME,dOb,POSTCODE
            Jane,Doe,20120510,SW1A 1AA
            """
        );

        var result = ParseAll(content);

        Assert.Single(result);
        Assert.Equal("Jane", result[0].Given);
        Assert.Equal(new DateOnly(2012, 5, 10), result[0].BirthDate);
    }

    [Fact]
    public async Task Should_Throw_When_RequiredHeadersAreMissing()
    {
        var content = CreateContent(
            """
            GivenName,FamilyName,DOB
            Jane,Doe,2012-05-10
            """
        );

        var exception = Assert.Throws<InvalidOperationException>(() => ParseAll(content));

        Assert.Contains("Postcode", exception.Message);
    }

    [Fact]
    public async Task Should_Throw_When_DobCannotBeParsed()
    {
        var content = CreateContent(
            """
            GivenName,FamilyName,DOB,Postcode
            Jane,Doe,not-a-date,SW1A 1AA
            """
        );

        Assert.Throws<InvalidOperationException>(() => ParseAll(content));
    }

    [Fact]
    public async Task Should_Throw_When_CsvDoesNotContainAnyRecords()
    {
        var content = CreateContent(
            """
            GivenName,FamilyName,DOB,Postcode
            """
        );

        Assert.Throws<InvalidOperationException>(() => ParseAll(content));
    }

    private static BinaryData CreateContent(string csv) => BinaryData.FromString(csv);

    private List<Shared.Models.PersonSpecification> ParseAll(BinaryData content) =>
        _sut.ParseListAsync(content, "test-file.csv", CancellationToken.None);
}
