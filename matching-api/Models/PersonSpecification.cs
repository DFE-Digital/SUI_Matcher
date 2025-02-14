using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MatchingApi.Models;

public class PersonSpecification
{
    [Required(ErrorMessage = "Given name is required")]
    [StringLength(20, ErrorMessage = "Name cannot be greater than 20 characters")]
    [JsonPropertyName("given")]
    public required string Given { get; set; }

    [Required(ErrorMessage = "Family name is required")]
    [StringLength(20, ErrorMessage = "Name cannot be greater than 20 characters")]
    [JsonPropertyName("family")]
    public required string Family { get; set; }

    [Required(ErrorMessage = "Date of birth is required")]
    [DataType(DataType.Date, ErrorMessage = "Invalid date of birth")]
    [JsonPropertyName("birthdate")]
    public required DateTime BirthDate { get; set; }

    [AllowedValues("male", "female", "unknown", "other", null, ErrorMessage = "Gender has to match FHIR standards" )]
    [JsonPropertyName("gender")]
    public required string Gender { get; set; }

    [Phone(ErrorMessage = "Invalid phone number.")]
    [JsonPropertyName("phone")]
    public required string Phone { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email address.")]
    [JsonPropertyName("email")]
    public required string Email { get; set; }

    [RegularExpression("^(([A-Z][0-9]{1,2})|(([A-Z][A-HJ-Y][0-9]{1,2})|(([A-Z][0-9][A-Z])|([A-Z][A-HJ-Y][0-9]?[A-Z])))) [0-9][A-Z]{2}$", ErrorMessage = "Invalid postcode.")]
    [JsonPropertyName("addresspostalcode")]
    public required string AddressPostalCode { get; set; }
}
