using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

using Microsoft.AspNetCore.Mvc;

using Shared.Converter;

namespace Shared.Models;

public class ReconciliationRequest : SearchSpecification
{
    [Required(ErrorMessage = "NHS number is required")]
    [StringLength(10, MinimumLength = 10, ErrorMessage = "NHS number must be 10 characters long")]
    [JsonPropertyName("nhsNumber")]
    public string? NhsNumber { get; set; }


    [JsonIgnore]
    public string ReconciliationId
    {
        get
        {
            var data = string.Join("|", 
                NhsNumber, Given, Family, BirthDate, Gender, AddressPostalCode, Email, Phone);

            byte[] bytes = Encoding.UTF8.GetBytes(data);
            byte[] hashBytes = SHA256.HashData(bytes);

            return Convert.ToHexString(hashBytes);
        }
    }
}