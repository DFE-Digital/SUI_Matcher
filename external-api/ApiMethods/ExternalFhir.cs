using Shared.Endpoint;
using Shared.OpenTelemetry;


namespace ExternalApi.ApiMethods;

public class ExternalFhirEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder app)
	{
		app.MapGet("/search", () => 
		{
			var value = "Person is Matched!!";
			return value;
		});
	}

}