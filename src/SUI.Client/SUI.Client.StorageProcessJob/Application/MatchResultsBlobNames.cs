namespace SUI.Client.StorageProcessJob.Application;

public sealed record MatchResultsBlobNames(
    string OriginalBlobName,
    string FullResultsBlobName,
    string SuccessResultsBlobName
);
