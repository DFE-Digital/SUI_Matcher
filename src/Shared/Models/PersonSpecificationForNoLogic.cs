namespace Shared.Models;

public class PersonSpecificationForNoLogic : PersonSpecification
{
    [JsonPropertyName("fuzzymatch")]
    public bool FuzzyMatch { get; set; } = false;
    [JsonPropertyName("exactmatch")]
    public bool ExactMatch { get; set; } = false;
}