using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc;

using Shared.Converter;

namespace Shared.Models;

public class ReconciliationRequest : PersonSpecification
{
    [Required(ErrorMessage = "NHS number is required")]
    [StringLength(10, MinimumLength = 10, ErrorMessage = "NHS number must be 10 characters long")]
    [JsonPropertyName("nhsNumber")]
    public string? NhsNumber { get; set; }
}