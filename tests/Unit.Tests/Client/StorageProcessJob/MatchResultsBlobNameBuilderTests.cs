using Microsoft.Extensions.Time.Testing;
using SUI.Client.StorageProcessJob.Application;

namespace Unit.Tests.Client.StorageProcessJob;

public class MatchResultsBlobNameBuilderTests
{
    private readonly MatchResultsBlobNameBuilder _sut;

    public MatchResultsBlobNameBuilderTests()
    {
        var timeProvider = new FakeTimeProvider(
            new DateTimeOffset(2026, 1, 20, 12, 0, 0, TimeSpan.Zero)
        );

        _sut = new MatchResultsBlobNameBuilder(timeProvider);
    }

    [Fact]
    public void Should_BuildArchivedOriginalBlobName_When_SourceBlobNameIsProvided()
    {
        var result = _sut.BuildArchivedOriginalBlobName("test-file.csv");

        Assert.Equal("20260120120000_test-file/test-file.csv", result);
    }

    [Fact]
    public void Should_BuildFullResultsBlobName_When_SourceBlobNameIsProvided()
    {
        var result = _sut.BuildFullResultsBlobName("test-file.csv");

        Assert.Equal("20260120120000_test-file/test-file_full-results.csv", result);
    }

    [Fact]
    public void Should_BuildSuccessResultsBlobName_When_SourceBlobNameIsProvided()
    {
        var result = _sut.BuildSuccessResultsBlobName("test-file.csv");

        Assert.Equal("20260120120000_test-file/test-file_success.csv", result);
    }

    [Fact]
    public void Should_UseOnlyFilename_When_SourceBlobNameContainsNestedPath()
    {
        var archivedOriginalBlobName = _sut.BuildArchivedOriginalBlobName(
            "incoming/folder/test-file.csv"
        );
        var fullResultsBlobName = _sut.BuildFullResultsBlobName("incoming/folder/test-file.csv");
        var successResultsBlobName = _sut.BuildSuccessResultsBlobName(
            "incoming/folder/test-file.csv"
        );

        Assert.Equal("20260120120000_test-file/test-file.csv", archivedOriginalBlobName);
        Assert.Equal("20260120120000_test-file/test-file_full-results.csv", fullResultsBlobName);
        Assert.Equal("20260120120000_test-file/test-file_success.csv", successResultsBlobName);
    }
}
