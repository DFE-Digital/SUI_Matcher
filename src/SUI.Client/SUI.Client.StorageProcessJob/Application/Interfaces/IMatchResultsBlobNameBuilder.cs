namespace SUI.Client.StorageProcessJob.Application.Interfaces;

public interface IMatchResultsBlobNameBuilder
{
    string BuildArchivedOriginalBlobName(string sourceBlobName);

    string BuildFullResultsBlobName(string sourceBlobName);

    string BuildSuccessResultsBlobName(string sourceBlobName);
}
