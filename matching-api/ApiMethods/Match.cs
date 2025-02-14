using Json.More;
using MatchingApi.Lib;
using MatchingApi.Models;
using MatchingApi.Services;
using Shared.Endpoint;

namespace MatchingApi.ApiMethods;

public class MatchEndpoint(ExternalServiceClient externalServiceClient) : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder app)
	{
		app.MapPost("/matchperson", async (PersonSpecification personSpecification) =>
		{
			var validationResults = ValidationUtil.Validate(personSpecification);

			if (validationResults.Results!.Any())
			{
				return Results.BadRequest(new
				{
					Results = validationResults.Results!.Select(vr => new
					{
						vr.MemberNames,
						vr.ErrorMessage
					})
				});
			}
			
			var searchResult = await externalServiceClient.PerformQuery(personSpecification);
			
			var value = "Person is Matched!!";
			return Results.Ok(value);
		});
	}

}