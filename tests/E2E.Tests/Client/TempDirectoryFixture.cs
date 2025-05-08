namespace E2E.Tests.Client;

public class TempDirectoryFixture : IAsyncLifetime
{
    public string BaseDirectoryPath { get; private set; } = null!;

    public string IncomingDirectoryPath => Path.Combine(BaseDirectoryPath, "Incoming");

    public string ProcessedDirectoryPath => Path.Combine(BaseDirectoryPath, "Processed");

    public Task InitializeAsync()
    {
        var tempRoot = Path.GetTempPath();
        var uniqueDirName = "Test_" + Guid.NewGuid().ToString("N");
        BaseDirectoryPath = Path.Combine(tempRoot, uniqueDirName);

        Directory.CreateDirectory(BaseDirectoryPath);
        Directory.CreateDirectory(IncomingDirectoryPath);
        Directory.CreateDirectory(ProcessedDirectoryPath);

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        Directory.Delete(BaseDirectoryPath, recursive: true);
        return Task.CompletedTask;
    }
}