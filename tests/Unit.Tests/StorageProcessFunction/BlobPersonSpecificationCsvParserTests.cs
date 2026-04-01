using Shared.Models;
using SUI.StorageProcessFunction.Application;

namespace Unit.Tests.StorageProcessFunction;

public class BlobPersonSpecificationCsvParserTests
{
    private readonly BlobPersonSpecificationCsvParser _sut = new();

    [Fact]
    public async Task Should_MapSingleCsvRowToPersonSpecification_When_HeadersMatch()
    {
        var blobFile = CreateBlobFile(
            """
            GivenName,FamilyName,DOB,Postcode
            Jane,Doe,2012-05-10,SW1A 1AA
            """
        );

        var result = await _sut.ParseAsync(blobFile, CancellationToken.None);

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
        var blobFile = CreateBlobFile(
            """
            GivenName,FamilyName,DOB,Postcode
            Jane,Doe,2012-05-10,SW1A 1AA
            John,Smith,2011/04/09,AB1 2CD
            """
        );

        var result = await _sut.ParseAsync(blobFile, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("Jane", result[0].Given);
        Assert.Equal("John", result[1].Given);
        Assert.Equal(new DateOnly(2011, 4, 9), result[1].BirthDate);
    }

    [Fact]
    public async Task Should_AcceptMixedCaseHeaders_When_HeaderCasingVaries()
    {
        var blobFile = CreateBlobFile(
            """
            givenname,FAMILYNAME,dOb,POSTCODE
            Jane,Doe,20120510,SW1A 1AA
            """
        );

        var result = await _sut.ParseAsync(blobFile, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Jane", result[0].Given);
        Assert.Equal(new DateOnly(2012, 5, 10), result[0].BirthDate);
    }

    [Fact]
    public async Task Should_Throw_When_RequiredHeadersAreMissing()
    {
        var blobFile = CreateBlobFile(
            """
            GivenName,FamilyName,DOB
            Jane,Doe,2012-05-10
            """
        );

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ParseAsync(blobFile, CancellationToken.None)
        );

        Assert.Contains("Postcode", exception.Message);
    }

    [Fact]
    public async Task Should_Throw_When_DobCannotBeParsed()
    {
        var blobFile = CreateBlobFile(
            """
            GivenName,FamilyName,DOB,Postcode
            Jane,Doe,not-a-date,SW1A 1AA
            """
        );

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ParseAsync(blobFile, CancellationToken.None)
        );
    }

    [Fact]
    public async Task Should_Throw_When_CsvDoesNotContainAnyRecords()
    {
        var blobFile = CreateBlobFile(
            """
            GivenName,FamilyName,DOB,Postcode
            """
        );

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ParseAsync(blobFile, CancellationToken.None)
        );
    }

    private static BlobFileContent CreateBlobFile(string csv) =>
        new(
            new StorageBlobMessage { ContainerName = "incoming", BlobName = "test-file.csv" },
            BinaryData.FromString(csv),
            "text/csv"
        );
}
