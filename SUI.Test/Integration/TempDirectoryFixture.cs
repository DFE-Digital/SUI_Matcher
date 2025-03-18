namespace SUI.Test.Integration;

public class TempDirectoryFixture
{
    public TempDirectoryFixture()
    {
        var tempRoot = Path.GetTempPath();
        var uniqueDirName = "Test_" + Guid.NewGuid().ToString("N");
        BaseDirectoryPath = Path.Combine(tempRoot, "SUI_client_tests", uniqueDirName);

        Directory.CreateDirectory(BaseDirectoryPath);
        Directory.CreateDirectory(IncomingDirectoryPath);
        Directory.CreateDirectory(ProcessedDirectoryPath);
    }

    public string BaseDirectoryPath { get; private set; } = null!;

    public string IncomingDirectoryPath => Path.Combine(BaseDirectoryPath, "Incoming");

    public string ProcessedDirectoryPath => Path.Combine(BaseDirectoryPath, "Processed");
    public void Dispose()
    {
        Directory.Delete(BaseDirectoryPath, recursive: true);
    }
}