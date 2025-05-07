using SUI.Client.Core.Models;

namespace SUI.Client.Core;


public class CsvMappingConfig
{
    public static class Defaults
    {
        public const string GivenName = "GivenName";
        public const string Surname = "Surname";
        public const string DOB = "DOB";
        public const string Email = "Email";
        public const string AddressPostalCode = "PostCode";
        public const string Gender = "Gender";
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