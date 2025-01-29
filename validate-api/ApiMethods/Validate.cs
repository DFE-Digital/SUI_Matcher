using Shared.Endpoint;
using Shared.OpenTelemetry;


namespace ValidateApi.ApiMethods;

public class ValidateEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder app)
	{
		app.MapGet("/runvalidation", () => 
		{
			var value = "Data is Valid!!";
			return value;
		});
	}

}