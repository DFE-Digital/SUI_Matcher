using SUI.Client.Core.Models;

namespace SUI.Client.Core;

public class CsvMappingConfig
{
    public Dictionary<string, string> ColumnMappings { get; set; } = new Dictionary<string, string>
    {
        [nameof(MatchPersonPayload.Given)] = "GivenName",
        [nameof(MatchPersonPayload.Family)] = "Surname",
        [nameof(MatchPersonPayload.BirthDate)] = "DOB",
        [nameof(MatchPersonPayload.Email)] = "Email",
    };
}
