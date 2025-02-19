using Shared.Models;
using SUI.Core.Domain;
using SUI.Core.Endpoints;

namespace SUI.Core.Services;

public interface IMatchingService
{
    Task<dynamic> SearchAsync(PersonSpecification personSpecification);
}

public class MatchingService(INhsFhirClient nhsFhirClient, IValidationService validationService) : IMatchingService
{
	public async Task<dynamic/*dynamic for now...*/> SearchAsync(PersonSpecification personSpecification)
	{
        var validationResults = validationService.Validate(personSpecification);

        if (validationResults.Results!.Any())
        {
            return validationResults;
        }

        var query = new SearchQuery
        {
            Given = [personSpecification.Given!],
            Family = personSpecification.Family,
            Email = personSpecification.Email,
            Gender = personSpecification.Gender,
            Phone = personSpecification.Phone,
            Birthdate = ["eq" + personSpecification.BirthDate.ToString("yyyy-MM-dd")],
            AddressPostalcode = personSpecification.AddressPostalCode
        };

        var searchResult = await nhsFhirClient.PerformSearch(query);

        var value = "Person is Matched!!";

        return value;
    }

}
