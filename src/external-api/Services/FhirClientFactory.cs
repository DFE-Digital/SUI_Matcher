using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;

using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;

namespace ExternalApi.Services;

public interface IFhirClientFactory
{
    FhirClient CreateFhirClient();
}

[ExcludeFromCodeCoverage(Justification = "Cannot test third party library")]
public class FhirClientFactory(ILogger<FhirClientFactory> logger, ITokenService tokenService, IConfiguration config) : IFhirClientFactory
{
    public FhirClient CreateFhirClient()
    {
        var baseUri = config["NhsAuthConfig:NHS_DIGITAL_FHIR_ENDPOINT"];
        var fhirClient = new FhirClient(baseUri);

        // Set the authorization header
        if (fhirClient.RequestHeaders != null)
        {
            var accessToken = tokenService.GetBearerToken().Result;
            fhirClient.RequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            fhirClient.RequestHeaders.Add("X-Request-ID", Guid.NewGuid().ToString());
        }

        return fhirClient;
    }
}