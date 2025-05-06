using System.Text.Json.Serialization;

namespace Shared.Models;

public class PersonMatchResponse
{
    [JsonPropertyName("result")]
    public MatchResult? Result { get; set; }

    [JsonPropertyName("dataQuality")]
    public DataQualityResult? DataQuality { get; set; }
}