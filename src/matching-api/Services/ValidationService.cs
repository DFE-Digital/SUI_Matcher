using System.ComponentModel.DataAnnotations;

using Shared.Endpoint;
using Shared.Models;

namespace MatchingApi.Services;

public class ValidationService : IValidationService
{
    public ValidationResponse Validate(object obj)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(obj, null, null);
        Validator.TryValidateObject(obj, validationContext, validationResults, true);

        var response = new ValidationResponse
        {
            Results = validationResults.Select(result => new ValidationResponse.ValidationResult
            {
                MemberNames = result.MemberNames,
                ErrorMessage = result.ErrorMessage
            }).ToList()
        };

        return response;
    }

    public ValidationResponse ValidateNhsNumber(string nhsNumber)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(nhsNumber, null, null);
        Validator.TryValidateObject(nhsNumber, validationContext, validationResults, true);

        var response = new ValidationResponse
        {
            Results = validationResults.Select(result => new ValidationResponse.ValidationResult
            {
                MemberNames = result.MemberNames,
                ErrorMessage = result.ErrorMessage
            }).ToList()
        };

        return response;
    }
}