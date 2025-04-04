using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace SUI.Core.Domain;

public record struct DemographicRequest
{
    [FromQuery(Name = "nhsNumber")]
    [Required(ErrorMessage = "NHS number is required")]
    [StringLength(10, MinimumLength = 10, ErrorMessage = "NHS number must be 10 characters long")]
    public string? NhsNumber { get; set; }
}