using System.Diagnostics.CodeAnalysis;

using Shared.Endpoint;
using Shared.Models;

namespace ExternalApi;

[ExcludeFromCodeCoverage(Justification = "Simple endpoint mapping")]
public class ExternalFhirEndpoint(INhsFhirClient fhirClient) : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var configuration = app.ServiceProvider.GetRequiredService<IConfiguration>();
        
        var search = app.MapPost("/search", async (SearchQuery query) => await fhirClient.PerformSearch(query));
        if (configuration.GetValue<bool>("EnableAuth"))
        {
            search.RequireAuthorization("AuthPolicy");
        }
        
        var demographics = app.MapGet("/demographics/{nhsId}", async (string nhsId) => await fhirClient.PerformSearchByNhsId(nhsId));
        if (configuration.GetValue<bool>("EnableAuth"))
        {
            demographics.RequireAuthorization("AuthPolicy");
        }
    }
}