using System;
using System.ComponentModel.DataAnnotations;

namespace ValidateApi.Models;

public class PersonSpecification
{
    [Required(ErrorMessage = "Given name is required")]
    [StringLength(20, ErrorMessage = "Name connot be greater than 20 characters")]
    public string Given { get; set; }

    [Required(ErrorMessage = "Family name is required")]
    [StringLength(20, ErrorMessage = "Name connot be greater than 20 characters")]
    public string Family { get; set; }

    [Required(ErrorMessage = "Date of birth is required")]
    [DataType(DataType.Date, ErrorMessage = "Invalid date of birth")] 
    public DateTime BirthDate { get; set; }

    [AllowedValues("male", "female", "unknown", "other", null, ErrorMessage = "Gender has to match FHIR standards" )]
    public string Gender { get; set; }

    [Phone(ErrorMessage = "Invalid phone number.")]
    public string Phone { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email address.")]
    public string Email { get; set; }

    [RegularExpression("^(([A-Z][0-9]{1,2})|(([A-Z][A-HJ-Y][0-9]{1,2})|(([A-Z][0-9][A-Z])|([A-Z][A-HJ-Y][0-9]?[A-Z])))) [0-9][A-Z]{2}$", ErrorMessage = "Invalid postcode.")]
    public string AddressPostalCode { get; set; }
}
