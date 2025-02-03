using Shared.Endpoint;
using Shared.OpenTelemetry;


namespace MatchingApi.ApiMethods;

public class MatchEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder app)
	{
		app.MapGet("/matchperson", () => 
		{
			var value = "Person is Matched!!";
			return value;
		});
	}

}