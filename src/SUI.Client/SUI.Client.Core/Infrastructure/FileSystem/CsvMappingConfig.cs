using Shared.Models;

using SUI.Client.Core.Application.Interfaces;

namespace SUI.Client.Core.Infrastructure.FileSystem;


public class CsvMappingConfig
{
    public Dictionary<string, List<string>> ColumnMappings { get; init; } = new()
    {
        [nameof(MatchPersonPayload.Given)] = ["GivenName", "Forename", "Given"],
        [nameof(MatchPersonPayload.Family)] = ["Surname", "FamilyName", "Family"],
        [nameof(MatchPersonPayload.BirthDate)] = ["DOB", "BirthDate"],
        [nameof(MatchPersonPayload.Email)] = ["Email"],
        [nameof(MatchPersonPayload.AddressPostalCode)] = ["PostCode", "PostalCode"],
        [nameof(MatchPersonPayload.Gender)] = ["Gender"],
        [nameof(ReconciliationRequest.NhsNumber)] = ["NhsNumber", "NHSNumber"],
        [nameof(ReconciliationRequest.Phone)] = ["Phone", "PhoneNumber"],
    };
}