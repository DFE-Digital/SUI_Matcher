using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SUI.Core.Domain;

public class PersonSpecification
{
    [Required(ErrorMessage = "Given name is required")]
    [StringLength(20, ErrorMessage = "Name cannot be greater than 20 characters")]
    [JsonPropertyName("given")]
    public string? Given { get; set; }

    [Required(ErrorMessage = "Family name is required")]
    [StringLength(20, ErrorMessage = "Name cannot be greater than 20 characters")]
    [JsonPropertyName("family")]
    public string? Family { get; set; }

    [Required(ErrorMessage = "Date of birth is required")]
    [DataType(DataType.Date, ErrorMessage = "Invalid date of birth")]
    [JsonPropertyName("birthdate")]
    public DateTime BirthDate { get; set; }

    [AllowedValues("male", "female", "unknown", "other", null, ErrorMessage = "Gender has to match FHIR standards" )]
    [JsonPropertyName("gender")]
    public string? Gender { get; set; }

    [Phone(ErrorMessage = "Invalid phone number.")]
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email address.")]
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [RegularExpression("^(([A-Z][0-9]{1,2})|(([A-Z][A-HJ-Y][0-9]{1,2})|(([A-Z][0-9][A-Z])|([A-Z][A-HJ-Y][0-9]?[A-Z])))) [0-9][A-Z]{2}$", ErrorMessage = "Invalid postcode.")]
    [JsonPropertyName("addresspostalcode")]
    public string? AddressPostalCode { get; set; }
}
