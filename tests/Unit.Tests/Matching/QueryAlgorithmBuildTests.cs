using MatchingApi.Util;

using Newtonsoft.Json;

using AlgoVersion = MatchingApi.Util.AlgorithmVersionUtil.AlgoVersion;

namespace Unit.Tests.Matching;

public sealed class QueryAlgorithmBuildTests
{
    [Fact]
    public void KeepVersionIfAlgorithmIsSame()
    {
        // Arrange
        var versionFile = Path.Combine("Algorithm", "version.json");

        File.WriteAllText(versionFile, JsonConvert.SerializeObject(new AlgoVersion
        {
            Version = 1,
            Hash = AlgorithmVersionUtil.ComputeQueryAlgorithmHash(),
            PreviousHash = ""
        }));

        // Act
        AlgorithmVersionUtil.StoreOrIncrementAlgorithmVersion();

        // Assert
        var latestVersionJson = JsonConvert.DeserializeObject<AlgoVersion>(File.ReadAllText(versionFile));
        Assert.Equal(1, latestVersionJson?.Version);
    }

    [Fact]
    public void IncrementVersionIfAlgorithmIsDifferent()
    {
        // Arrange
        var versionFile = Path.Combine("Algorithm", "version.json");

        File.WriteAllText(versionFile, JsonConvert.SerializeObject(new AlgoVersion
        {
            Version = 1,
            Hash = "oldAlgorithm",
            PreviousHash = "olderAlgorithm"
        }));

        // Act
        AlgorithmVersionUtil.StoreOrIncrementAlgorithmVersion();

        // Assert
        var latestVersionJson = JsonConvert.DeserializeObject<AlgoVersion>(File.ReadAllText(versionFile));
        Assert.Equal(2, latestVersionJson?.Version);
    }

    [Fact]
    public void DecrementVersionIfAlgorithmIsSameAsPrevious()
    {
        // Arrange
        var versionFile = Path.Combine("Algorithm", "version.json");

        File.WriteAllText(versionFile, JsonConvert.SerializeObject(new AlgoVersion
        {
            Version = 2,
            Hash = "latestAlgorithm",
            PreviousHash = AlgorithmVersionUtil.ComputeQueryAlgorithmHash()
        }));

        // Act
        AlgorithmVersionUtil.StoreOrIncrementAlgorithmVersion();

        // Assert
        var latestVersionJson = JsonConvert.DeserializeObject<AlgoVersion>(File.ReadAllText(versionFile));
        Assert.Equal(1, latestVersionJson?.Version);
    }

    [Fact]
    public void CurrentAlgorithmVersionReturned()
    {
        // Arrange

        // Act
        var versionFile = Path.Combine("Algorithm", "version.json");

        File.WriteAllText(versionFile, JsonConvert.SerializeObject(new AlgoVersion
        {
            Version = 4,
            Hash = "latestAlgorithm",
            PreviousHash = AlgorithmVersionUtil.ComputeQueryAlgorithmHash()
        }));

        // Assert
        var latestVersion = AlgorithmVersionUtil.GetCurrentAlgorithmVersion();
        Assert.Equal(4, latestVersion);
    }
}