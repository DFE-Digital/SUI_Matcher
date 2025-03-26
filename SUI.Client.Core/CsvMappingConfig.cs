using SUI.Client.Core.Models;

namespace SUI.Client.Core;

public class CsvMappingConfig
{
    public static class Defaults
    {
        public const string GivenName = "GIVEN_NAME";
        public const string Surname = "FAMILY_NAME";
        public const string DOB = "DOB";
        public const string Email = "EMAIL";
        public const string AddressPostalCode = "POST_CODE";
        public const string Gender = "GENDER";
    }

    public Dictionary<string, string> ColumnMappings { get; set; } = new Dictionary<string, string>
    {
        [nameof(MatchPersonPayload.Given)] = Defaults.GivenName,
        [nameof(MatchPersonPayload.Family)] = Defaults.Surname,
        [nameof(MatchPersonPayload.BirthDate)] = Defaults.DOB,
        [nameof(MatchPersonPayload.Email)] = Defaults.Email,
        [nameof(MatchPersonPayload.AddressPostalCode)] = Defaults.AddressPostalCode,
        [nameof(MatchPersonPayload.Gender)] = Defaults.Gender,
    };
}
