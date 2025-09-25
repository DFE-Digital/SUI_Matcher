using Shared.Models;

namespace Shared.Endpoint;

public interface IMatchingService
{
    Task<PersonMatchResponse> SearchAsync(SearchSpecification searchSpecification);
    Task<PersonMatchResponse> SearchNoLogicAsync(PersonSpecificationForNoLogic personSpecification);
    Task<DemographicResponse?> GetDemographicsAsync(DemographicRequest request);
}