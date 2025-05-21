using System.Diagnostics.CodeAnalysis;

using Shared.Endpoint;
using Shared.Models;

namespace ExternalApi;

[ExcludeFromCodeCoverage(Justification = "Simple endpoint mapping")]
public class ExternalFhirEndpoint(INhsFhirClient fhirClient) : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/search", async (SearchQuery query) => await fhirClient.PerformSearch(query));

        app.MapGet("/demographics/{nhsId}", async (string nhsId) => await fhirClient.PerformSearchByNhsId(nhsId));
    }
}