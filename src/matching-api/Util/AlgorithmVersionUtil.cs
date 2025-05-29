using System.Security.Cryptography;
using System.Text;

using MatchingApi.Services;

using Newtonsoft.Json;

using Shared.Models;

namespace MatchingApi.Util;

public static class AlgorithmVersionUtil
{
    private const string AlgorithmVersionDirectory = "Algorithm";
    private static readonly string AlgorithmVersionFilePath = Path.Combine(AlgorithmVersionDirectory, "version.json");

    public static int? GetCurrentAlgorithmVersion()
    {
        return File.Exists(AlgorithmVersionFilePath)
            ? JsonConvert.DeserializeObject<AlgoVersion>(File.ReadAllText(AlgorithmVersionFilePath))?.Version
            : null;
    }

    public static void StoreOrIncrementAlgorithmVersion()
    {
        if (!Directory.Exists(AlgorithmVersionDirectory))
            Directory.CreateDirectory(AlgorithmVersionDirectory);

        string newHash = ComputeQueryAlgorithmHash();

        var versionJson = File.Exists(AlgorithmVersionFilePath) ?
            JsonConvert.DeserializeObject<AlgoVersion>(File.ReadAllText(AlgorithmVersionFilePath)) : new AlgoVersion();

        string currentHash = versionJson?.Hash ?? string.Empty;
        string prevHash = versionJson?.PreviousHash ?? string.Empty;
        int version = versionJson?.Version ?? 1;

        if (newHash == currentHash) // keep version the same
        {
            Console.WriteLine($"No changes detected, version remains {version}");
        }
        else
        {
            if (newHash == prevHash) // revert to previous version
            {
                --version;
            }
            else if (newHash != currentHash) // increment version
            {
                ++version;
            }

            File.WriteAllText(AlgorithmVersionFilePath, JsonConvert.SerializeObject(
                new AlgoVersion
                {
                    Version = version,
                    Hash = newHash,
                    PreviousHash = currentHash,
                }, Formatting.Indented));

            Console.WriteLine($"Version updated to {version}");
        }
    }

    public static string ComputeQueryAlgorithmHash()
    {
        var queries = MatchingService.GetSearchQueries(new PersonSpecification()
        {
            BirthDate = new DateOnly(1970, 1, 1)
        });
        byte[] bytes = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(queries));
        byte[] hashBytes = SHA3_256.HashData(bytes);

        StringBuilder builder = new();
        for (int i = 0; i < hashBytes.Length; i++)
        {
            builder.Append(hashBytes[i].ToString("x2"));
        }

        return builder.ToString();
    }

    public class AlgoVersion
    {
        [JsonProperty("version")]
        public int Version { get; set; }

        [JsonProperty("hash")] public string Hash { get; set; } = "";

        [JsonProperty("previousHash")] public string PreviousHash { get; set; } = "";
    }
}