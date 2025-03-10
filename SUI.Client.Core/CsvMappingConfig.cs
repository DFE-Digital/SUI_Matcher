using SUI.Client.Core.Models;

namespace SUI.Client.Core;

public class CsvMappingConfig
{
    public static class Defaults
    {
        public const string GivenName = nameof(GivenName);
        public const string Surname = nameof(Surname);
        public const string DOB = nameof(DOB);
        public const string Email = nameof(Email);
    }

    public Dictionary<string, string> ColumnMappings { get; set; } = new Dictionary<string, string>
    {
        [nameof(MatchPersonPayload.Given)] = Defaults.GivenName,
        [nameof(MatchPersonPayload.Family)] = Defaults.Surname,
        [nameof(MatchPersonPayload.BirthDate)] = Defaults.DOB,
        [nameof(MatchPersonPayload.Email)] = Defaults.Email,
    };
}
