using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using Hl7.Fhir.Rest;

namespace ExternalApi.Services;

public interface IFhirClientFactory
{
    FhirClient CreateFhirClient();
}

[ExcludeFromCodeCoverage(Justification = "Cannot test third party library")]
public class FhirClientFactory(
    ILogger<FhirClientFactory> logger,
    ITokenService tokenService,
    IConfiguration config
) : IFhirClientFactory
{
    public FhirClient CreateFhirClient()
    {
        var baseUri = config["NhsAuthConfig:NHS_DIGITAL_FHIR_ENDPOINT"];
        var fhirClient = new FhirClient(baseUri);

        // Set the authorization header
        if (fhirClient.RequestHeaders != null)
        {
            var accessToken = tokenService.GetBearerToken().Result;
            fhirClient.RequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                accessToken
            );

            // This traceRequestId is not sensitive and is only used for tracing between us and PDS
            var pdsTraceRequestId = Guid.NewGuid().ToString();
#pragma warning disable CA1873
            // Not necessary, but useful if the AddTag method is not working for some reason.
            logger.LogInformation("PDS Trace Request ID: {TraceRequestId}", pdsTraceRequestId);
#pragma warning restore CA1873
            // This will show on dependency logs in Application Insights and can be used to correlate requests between us and PDS
            Activity.Current?.AddTag("PdsTraceRequestId", pdsTraceRequestId);
            fhirClient.RequestHeaders.Add("X-Request-ID", pdsTraceRequestId);
        }

        return fhirClient;
    }
}
