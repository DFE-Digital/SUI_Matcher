using System.Reflection;
using System.Text.Json.Serialization;

namespace SUI.Core.Domain;

public class PersonMatchResponse
{
    [JsonPropertyName("result")]
    public MatchResult? Result { get; set; }
    
    [JsonPropertyName("dataQuality")]
    public DataQualityResult? DataQuality { get; set; }

    public class MatchResult
    {
        [JsonPropertyName("matchStatus")]
        public MatchStatus MatchStatus { get; set; }
        
        [JsonPropertyName("nhsNumber")]
        public string? NhsNumber { get; set; }
        
        [JsonPropertyName("processStage")]
        public int? ProcessStage { get; set; }
        
        [JsonPropertyName("score")]
        public decimal? Score { get; set; }
    }

    public class DataQualityResult
    {
        [JsonPropertyName("given")] 
        public QualityType Given { get; set; } = QualityType.Valid;
        
        [JsonPropertyName("family")] 
        public QualityType Family { get; set; } = QualityType.Valid;
        
        [JsonPropertyName("birthdate")] 
        public QualityType Birthdate { get; set; } = QualityType.Valid;
        
        [JsonPropertyName("addressPostalCode")]
        public QualityType AddressPostalCode { get; set; } = QualityType.Valid;

        [JsonPropertyName("phone")] 
        public QualityType Phone { get; set; } = QualityType.Valid;

        [JsonPropertyName("email")] 
        public QualityType Email { get; set; } = QualityType.Valid;

        [JsonPropertyName("gender")] 
        public QualityType Gender { get; set; } = QualityType.Valid;

        public Dictionary<string, string> ToDictionary()
        {
            var jsonPropertyDict = new Dictionary<string, string>();

            var properties = GetType().GetProperties();
    
            foreach (var property in properties)
            {
                var jsonPropertyAttribute = property.GetCustomAttribute<JsonPropertyNameAttribute>();

                if (jsonPropertyAttribute == null) continue;
			
                var jsonPropertyName = jsonPropertyAttribute.Name;
                var value = property.GetValue(this);

                if (value is QualityType qualityType)
                {
                    jsonPropertyDict[jsonPropertyName] = qualityType.ToString();
                }
            }

            return jsonPropertyDict;
        }
    }

    public enum QualityType
    {
        Valid,
        Invalid,
        NotProvided
    }
}