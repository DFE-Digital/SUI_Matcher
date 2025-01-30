using Shared.Endpoint;
using Shared.OpenTelemetry;


namespace ExternalApi.ApiMethods;

public class ExternalAuthEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder app)
	{
		app.MapGet("/authnhs", () => 
		{
			var value = "Person is Matched!!";
			return value;
		});
	}

}