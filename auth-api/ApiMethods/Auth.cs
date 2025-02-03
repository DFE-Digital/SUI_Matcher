using Shared.Endpoint;
using Shared.OpenTelemetry;


namespace AuthApi.ApiMethods;

public class AuthhEndpoint : IEndpoint
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