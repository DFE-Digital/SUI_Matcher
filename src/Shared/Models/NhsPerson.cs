using Shared.Util;

namespace Shared.Models;

public class NhsPerson
{
    public required string NhsNumber { get; set; }

    public string[] GivenNames { get; set; } = [];

    public string[] FamilyNames { get; set; } = [];

    [JsonConverter(typeof(CustomDateOnlyConverter))]
    public DateOnly? BirthDate { get; set; }

    public string? Gender { get; set; }

    public string[] PhoneNumbers { get; set; } = [];

    public string[] Emails { get; set; } = [];
    public string[] AddressPostalCodes { get; set; } = [];

    public string[] AddressHistory { get; set; } = [];

    public string? GeneralPractitionerOdsId { get; set; }
}