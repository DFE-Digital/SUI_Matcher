using SUI.Client.Core.Infrastructure.FileSystem;

namespace Unit.Tests.Client;

public class CsvRecordReaderTests
{
    [Fact]
    public async Task Should_ReadHeadersAndRecords_When_CsvContainsData()
    {
        using var reader = new StringReader(
            """
            GivenName,FamilyName,DOB,Postcode
            Jane,Doe,2012-05-10,SW1A 1AA
            """
        );

        var (headers, records) = await CsvRecordReader.ReadCsvTextAsync(reader);

        Assert.Contains("GivenName", headers);
        Assert.Single(records);
        Assert.Equal("Jane", records[0]["GivenName"]);
        Assert.Equal("Doe", records[0]["FamilyName"]);
    }

    [Fact]
    public async Task Should_TreatHeadersAsCaseInsensitive_When_HeaderCasingVaries()
    {
        using var reader = new StringReader(
            """
            givenname,FAMILYNAME,dob,postcode
            Jane,Doe,2012-05-10,SW1A 1AA
            """
        );

        var (headers, records) = await CsvRecordReader.ReadCsvTextAsync(reader);

        Assert.Contains("GivenName", headers);
        Assert.Equal("Jane", records[0]["GivenName"]);
        Assert.Equal("Doe", records[0]["familyname"]);
        Assert.Equal("2012-05-10", records[0]["DOB"]);
        Assert.Equal("SW1A 1AA", records[0]["Postcode"]);
    }
}
