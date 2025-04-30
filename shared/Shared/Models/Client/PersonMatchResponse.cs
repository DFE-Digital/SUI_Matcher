using System.Text.Json.Serialization;
using Shared.Models.Client;

namespace Shared.Models.Client;

public class PersonMatchResponse
{
    [JsonPropertyName("result")]
    public MatchResult? Result { get; set; }

    [JsonPropertyName("dataQuality")]
    public DataQualityResult? DataQuality { get; set; }    
}
