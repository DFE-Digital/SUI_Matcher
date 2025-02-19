using System.Collections;
using System.Net.Http.Headers;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Shared.Models;

namespace ExternalApi.Services;

public class NhsFhirClient(
    ITokenService tokenService,
    ILogger<NhsFhirClient> logger,
    IConfiguration configuration)
{
    private readonly string _baseUri = configuration["NhsAuthConfig:NHS_DIGITAL_FHIR_ENDPOINT"]!;
    
    public async Task<SearchResult> PerformSearch(SearchQuery query)
    {
        var fhirClient = new FhirClient(_baseUri);

        var search = new SearchParams();
			
        var queryMap = query.ToDictionary();
        foreach (var entry in queryMap) {
            if (entry.Value.GetType().IsArray)
            {
                foreach (var item in (IEnumerable) entry.Value) {
                    search.Add(entry.Key, item.ToString()!);
                }
            }
            else
            {
                search.Add(entry.Key, entry.Value.ToString()!);
            }
        }

        // Set the authorization header
        if (fhirClient.RequestHeaders != null)
        {
            var accessToken = await tokenService.GetBearerToken();
            fhirClient.RequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            fhirClient.RequestHeaders.Add("X-Request-ID", Guid.NewGuid().ToString());
        }

        try
        {
            // Search for a patient record
            var patient = await fhirClient.SearchAsync<Patient>(search);

            if (patient == null || patient.Entry.Count == 0)
            {
                return new SearchResult
                {
                    Type = SearchResult.ResultType.Unmatched
                };
            }

            if (patient.Entry.Count == 1)
            {
                return new SearchResult
                {
                    Type = SearchResult.ResultType.Matched,
                    NhsNumber = patient.Entry[0].Resource.Id,
                    Score = patient.Entry[0].Search.Score
                };
            }
            
            return new SearchResult
            {
                Type = SearchResult.ResultType.MultiMatched
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);

            return new SearchResult
            {
                Type = SearchResult.ResultType.Error,
                ErrorMessage = ex.Message
            };
        }
    }
}