using Microsoft.AspNetCore.Mvc;
using Shared.Endpoint;
using SUI.Core.Domain;
using SUI.Core.Services;
using SUI.Types;

namespace MatchingApi;

public class MatchEndpoint(IMatchingService matchingService) : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder app)
	{
		app.MapPost("/matchperson", async (PersonSpecification? model) =>
		{
            if (model is null)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Validation error",
                    Detail = "Request payload is empty",
                });
            }

            var result = await matchingService.SearchAsync(model);

            return result.Result?.MatchStatus == MatchStatus.Error ? Results.BadRequest(result) : Results.Ok(result);
        });
    }
}
