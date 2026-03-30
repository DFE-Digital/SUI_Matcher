namespace SUI.StorageProcessFunction.Application;

public sealed record BlobFileContent(
    StorageBlobMessage Blob,
    BinaryData Content,
    string? ContentType
);
