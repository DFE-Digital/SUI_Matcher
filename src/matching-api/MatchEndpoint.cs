using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using MatchingApi.Exceptions;

using Microsoft.AspNetCore.Mvc;

using Shared.Endpoint;
using Shared.Models;

namespace MatchingApi;

[ExcludeFromCodeCoverage(Justification = "Simple endpoint mapping")]
public class MatchEndpoint(IMatchingService matchingService, IReconciliationService reconciliationService) : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var configuration = app.ServiceProvider.GetRequiredService<IConfiguration>();
        var matchPerson = app.MapPost("/matchperson", async (SearchSpecification? model) =>
        {
            if (model is null)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Validation error",
                    Detail = "Request payload is empty",
                });
            }

            try
            {
                var result = await matchingService.SearchAsync(model);

                return result.Result?.MatchStatus == MatchStatus.Error
                    ? Results.BadRequest(result)
                    : Results.Ok(result);
            }
            catch (InvalidStrategyException ex)
            {
                return Results.BadRequest(new ProblemDetails() { Title = "Strategy validation error", Detail = ex.Message });
            }
        });

        var matchPersonNoLogic = app.MapPost("/matchpersonnologic", async (PersonSpecificationForNoLogic? model) =>
        {
            if (model is null)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Validation error",
                    Detail = "Request payload is empty",
                });
            }

            var result = await matchingService.SearchNoLogicAsync(model);

            return result.Result?.MatchStatus == MatchStatus.Error ? Results.BadRequest(result) : Results.Ok(result);
        });

        var demographics = app.MapGet("/demographics", async ([AsParameters] DemographicRequest request) =>
        {
            var result = await matchingService.GetDemographicsAsync(request);
            return result is null ? Results.BadRequest(result) : Results.Ok(result);
        });

        var reconciliation = app.MapPost("/reconciliation", async (ReconciliationRequest? model) =>
        {
            if (model is null)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Validation error",
                    Detail = "Request payload is empty",
                });
            }

            Activity.Current?.SetBaggage("ReconciliationId", model.ReconciliationId);

            var result = await reconciliationService.ReconcileAsync(model);
            return result.Status == ReconciliationStatus.Error ? Results.BadRequest(result) : Results.Ok(result);
        });

        if (configuration.GetValue<bool>("EnableAuth"))
        {
            const string authPolicyName = "AuthPolicy";
            matchPerson.RequireAuthorization(authPolicyName);
            matchPersonNoLogic.RequireAuthorization(authPolicyName);
            demographics.RequireAuthorization(authPolicyName);
            reconciliation.RequireAuthorization(authPolicyName);
        }
    }
}