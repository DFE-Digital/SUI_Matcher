using System.ComponentModel.DataAnnotations;

namespace Shared.Models;

public class NhsPerson
{
    public required string NhsNumber { get; set; }

    public string[] GivenNames { get; set; } = [];

    public string[] FamilyNames { get; set; } = [];

    [DataType(DataType.Date, ErrorMessage = PersonValidationConstants.BirthDateInvalid)]
    [JsonPropertyName("birthdate")]
    public DateOnly? BirthDate { get; set; }

    public string? Gender { get; set; }

    public string[] PhoneNumbers { get; set; } = [];

    public string[] Emails { get; set; } = [];
    public string[] AddressPostalCodes { get; set; } = [];
}