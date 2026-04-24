using System.Globalization;

namespace SUI.Client.StorageProcessJob.Application;

public sealed class MatchResultsBlobNameBuilder(TimeProvider timeProvider)
{
    public MatchResultsBlobNames Build(string sourceBlobName)
    {
        var fileName = Path.GetFileName(sourceBlobName);
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(sourceBlobName);
        var baseDirectory = BuildBaseDirectory(fileNameWithoutExtension);

        return new MatchResultsBlobNames(
            $"{baseDirectory}/{fileName}",
            $"{baseDirectory}/{fileNameWithoutExtension}_full-results.csv",
            $"{baseDirectory}/{fileNameWithoutExtension}_success.csv"
        );
    }

    public string BuildArchivedOriginalBlobName(string sourceBlobName)
    {
        return Build(sourceBlobName).OriginalBlobName;
    }

    public string BuildFullResultsBlobName(string sourceBlobName)
    {
        return Build(sourceBlobName).FullResultsBlobName;
    }

    public string BuildSuccessResultsBlobName(string sourceBlobName)
    {
        return Build(sourceBlobName).SuccessResultsBlobName;
    }

    private string BuildBaseDirectory(string fileNameWithoutExtension)
    {
        var timestamp = timeProvider
            .GetUtcNow()
            .ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);

        return $"{timestamp}_{fileNameWithoutExtension}";
    }
}
