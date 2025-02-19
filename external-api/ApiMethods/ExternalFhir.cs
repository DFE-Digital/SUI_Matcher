using ExternalApi.Services;
using Shared.Endpoint;
using Shared.Models;

namespace ExternalApi.ApiMethods;

public class ExternalFhirEndpoint(NhsFhirClient fhirClient) : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder app)
	{
		app.MapPost("/search", async (SearchQuery query) => await fhirClient.PerformSearch(query));
	}
}