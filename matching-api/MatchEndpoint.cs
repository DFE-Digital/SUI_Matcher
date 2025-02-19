using Shared.Endpoint;
using SUI.Core.Domain;
using SUI.Core.Services;

namespace MatchingApi;

public class MatchEndpoint(IMatchingService matchingService) : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder app)
	{
		app.MapPost("/matchperson", async (PersonSpecification personSpecification) =>
		{
			return Results.Ok(await matchingService.SearchAsync(personSpecification));
        });
	}

}