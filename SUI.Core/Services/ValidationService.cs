using System.ComponentModel.DataAnnotations;

using SUI.Core.Domain;

namespace SUI.Core.Services;

public interface IValidationService
{
    ValidationResponse Validate(object obj);
}

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