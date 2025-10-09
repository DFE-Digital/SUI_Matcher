using Shared.Models;

namespace Shared.Endpoint;

public interface IMatchingService
{
    Task<PersonMatchResponse> SearchAsync(SearchSpecification searchSpecification, bool logMatch = true);
    Task<PersonMatchResponse> SearchNoLogicAsync(PersonSpecificationForNoLogic personSpecification);
    Task<DemographicResponse?> GetDemographicsAsync(DemographicRequest request);
}