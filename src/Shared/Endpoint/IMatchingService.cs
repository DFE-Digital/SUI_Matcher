using Shared.Models;

namespace Shared.Endpoint;

public interface IMatchingService
{
    Task<PersonMatchResponse> SearchAsync(PersonSpecification personSpecification);
    Task<DemographicResponse?> GetDemographicsAsync(DemographicRequest request);
}