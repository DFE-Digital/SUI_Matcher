using System.Globalization;

namespace SUI.Client.StorageProcessJob.Application;

public sealed class MatchResultsBlobNameBuilder(TimeProvider timeProvider)
{
    public string BuildArchivedOriginalBlobName(string sourceBlobName)
    {
        var processedDirectory = BuildBaseDirectory(sourceBlobName);
        var fileName = Path.GetFileName(sourceBlobName);

        return $"{processedDirectory}/{fileName}";
    }

    public string BuildFullResultsBlobName(string sourceBlobName)
    {
        var processedDirectory = BuildBaseDirectory(sourceBlobName);
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(sourceBlobName);

        return $"{processedDirectory}/{fileNameWithoutExtension}_full-results.csv";
    }

    public string BuildSuccessResultsBlobName(string sourceBlobName)
    {
        var processedDirectory = BuildBaseDirectory(sourceBlobName);
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(sourceBlobName);

        return $"{processedDirectory}/{fileNameWithoutExtension}_success.csv";
    }

    private string BuildBaseDirectory(string sourceBlobName)
    {
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(sourceBlobName);
        var timestamp = timeProvider
            .GetUtcNow()
            .ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);

        return $"{timestamp}_{fileNameWithoutExtension}";
    }
}
