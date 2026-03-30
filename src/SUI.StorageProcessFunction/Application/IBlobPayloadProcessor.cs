namespace SUI.StorageProcessFunction.Application;

public interface IBlobPayloadProcessor
{
    Task ProcessAsync(BlobFileContent blobFile, CancellationToken cancellationToken);
}
