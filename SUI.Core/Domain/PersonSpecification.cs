using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SUI.Core.Domain;

public static class PersonValidationConstants {
    public const string GivenNameRequired = "Given name is required"; 
    public const string GivenNameInvalid = "Given name cannot be greater than 20 characters"; 
    public const string FamilyNameRequired = "Family name is required"; 
    public const string FamilyNameInvalid = "Family name cannot be greater than 20 characters"; 
    public const string BirthDateRequired = "Date of birth is required"; 
    public const string BirthDateInvalid = "Invalid date of birth"; 
    public const string GenderInvalid = "Gender has to match FHIR standards";
    public const string PhoneInvalid = "Invalid phone number.";
    public const string EmailInvalid = "Invalid email address.";
    public const string PostCodeInvalid = "Invalid postcode.";
}

public class PersonSpecification
{
    [Required(ErrorMessage = PersonValidationConstants.GivenNameRequired)]
    [StringLength(20, ErrorMessage = PersonValidationConstants.GivenNameInvalid)]
    [JsonPropertyName("given")]
    public string? Given { get; set; }

    [Required(ErrorMessage = PersonValidationConstants.FamilyNameRequired)]
    [StringLength(20, ErrorMessage = PersonValidationConstants.FamilyNameInvalid)]
    [JsonPropertyName("family")]
    public string? Family { get; set; }

    [Required(ErrorMessage = PersonValidationConstants.BirthDateRequired)]
    [DataType(DataType.Date, ErrorMessage = PersonValidationConstants.BirthDateInvalid)]
    [JsonPropertyName("birthdate")]
    public DateOnly? BirthDate { get; set; }

    [AllowedValues("male", "female", "unknown", "other", null, ErrorMessage = PersonValidationConstants.GenderInvalid )]
    [JsonPropertyName("gender")]
    public string? Gender { get; set; }

    [Phone(ErrorMessage = PersonValidationConstants.PhoneInvalid)]
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = PersonValidationConstants.EmailInvalid)]
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [RegularExpression("^(([A-Z][0-9]{1,2})|(([A-Z][A-HJ-Y][0-9]{1,2})|(([A-Z][0-9][A-Z])|([A-Z][A-HJ-Y][0-9]?[A-Z])))) [0-9][A-Z]{2}$", ErrorMessage = PersonValidationConstants.PostCodeInvalid)]
    [JsonPropertyName("addresspostalcode")]
    public string? AddressPostalCode { get; set; }
}
