using System.ComponentModel.DataAnnotations;
using MatchingApi.Models;

namespace MatchingApi.Lib;

public static class ValidationUtil
{
    public static ValidationResponse Validate(PersonSpecification personSpecification)
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