using MatchingApi.Services;
using Shared.Endpoint;

namespace MatchingApi.ApiMethods;

public class MatchEndpoint(ExternalServiceClient externalServiceClient) : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder app)
	{
		app.MapGet("/matchperson", async () =>
		{
			await externalServiceClient.PerformQuery();
			
			var value = "Person is Matched!!";
			return value;
		});
	}

}