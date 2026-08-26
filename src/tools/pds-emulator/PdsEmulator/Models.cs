using System.Text.Json.Serialization;

namespace PdsEmulator;

public class Patient
{
    [JsonPropertyName("resourceType")]
    public string? ResourceType { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public List<HumanName>? Name { get; set; }

    [JsonPropertyName("birthDate")]
    public string? BirthDate { get; set; }

    [JsonPropertyName("gender")]
    public string? Gender { get; set; }

    [JsonExtensionData]
    public Dictionary<string, System.Text.Json.JsonElement>? ExtensionData { get; set; }
}

public class HumanName
{
    [JsonPropertyName("family")]
    public string? Family { get; set; }

    [JsonPropertyName("given")]
    public List<string>? Given { get; set; }

    [JsonExtensionData]
    public Dictionary<string, System.Text.Json.JsonElement>? ExtensionData { get; set; }
}