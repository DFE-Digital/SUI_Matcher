using SUI.Core.Domain;
using System.ComponentModel.DataAnnotations;

namespace SUI.Core.Services;

public interface IValidationService
{
    ValidationResponse Validate(PersonSpecification personSpecification);
}

public class ValidationService : IValidationService
{
    public ValidationResponse Validate(PersonSpecification personSpecification)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(personSpecification, null, null);
        Validator.TryValidateObject(personSpecification, validationContext, validationResults, true);

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
