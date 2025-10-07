using System.ComponentModel.DataAnnotations;

using Shared.Attributes;

namespace Shared.Models;

public sealed class DemographicsRequest
{
    [Required]
    [JsonPropertyName("nhs-number")]
    public required string NhsNumber { get; set; }
}