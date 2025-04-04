using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.Models;
using SUI.Core.Endpoints.AuthToken;
using System.Collections;
using System.Net.Http.Headers;

namespace SUI.Core.Endpoints;

public interface INhsFhirClient
{
    Task<SearchResult?> PerformSearch(SearchQuery query);
    Task<DemographicResult> PerformSearchByNhsId(string nhsId);
}

public class NhsFhirClient(ITokenService tokenService,
                           ILogger<NhsFhirClient> logger,
                           IConfiguration configuration) : INhsFhirClient
{
    private readonly string _baseUri = configuration["NhsAuthConfig:NHS_DIGITAL_FHIR_ENDPOINT"]!;

    public async Task<SearchResult?> PerformSearch(SearchQuery query)
    {
        var fhirClient = CreateFhirClient();

        var search = new SearchParams();

        var queryMap = query.ToDictionary();
        foreach (var entry in queryMap)
        {
            if (entry.Value.GetType().IsArray)
            {
                foreach (var item in (IEnumerable)entry.Value)
                {
                    search.Add(entry.Key, item.ToString()!);
                }
            }
            else
            {
                search.Add(entry.Key, entry.Value.ToString()!);
            }
        }

        try
        {
            logger.LogInformation("Searching for an Nhs patient record");
            
            // Search for a patient record
            var patient = await fhirClient.SearchAsync<Patient>(search);
            if (patient == null || patient.Entry.Count == 0)
            {
                logger.LogInformation("No patient record found");

                return SearchResult.Unmatched();
            }
            else if (patient.Entry.Count == 1)
            {
                var birthDate = patient.Entry[0].Resource["birthDate"];
                var gender = patient.Entry[0].Resource["gender"];
                
                logger.LogInformation($"1 patient record found: BirthDate={birthDate} Gender={gender}");

                return SearchResult.Match(patient.Entry[0].Resource.Id, patient.Entry[0].Search.Score);
            }

            logger.LogInformation("multiple patient records found");

            return SearchResult.MultiMatched();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while performing Nhs Digital FHIR API search");
            return SearchResult.Error(ex.Message);
        }
    }

    public async Task<DemographicResult> PerformSearchByNhsId(string nhsId)
    {
        try
        {
            var fhirClient = CreateFhirClient();

            var data = await fhirClient.ReadAsync<Patient>(ResourceIdentity.Build("Patient", nhsId));

            return new DemographicResult()
            {
                Result = data
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while performing Nhs Digital FHIR API search by NHS ID");
            return new DemographicResult()
            {
                ErrorMessage = "Error occurred while performing Nhs Digital FHIR API search by NHS ID"
            };
        }
    }
    
    private FhirClient CreateFhirClient()
    {
        var fhirClient = new FhirClient(_baseUri);
        
        // Set the authorization header
        if (fhirClient.RequestHeaders != null)
        {
            var accessToken = tokenService.GetBearerToken().Result;
            
            logger.LogInformation("Retrieved Nhs Digital FHIR API access token");
            
            fhirClient.RequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            fhirClient.RequestHeaders.Add("X-Request-ID", Guid.NewGuid().ToString());
        }

        return fhirClient;
    }
}