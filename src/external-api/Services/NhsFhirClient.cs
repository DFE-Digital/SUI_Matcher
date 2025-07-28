using System.Collections;
using System.Net.Http.Headers;

using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;

using Shared.Endpoint;
using Shared.Models;

namespace ExternalApi.Services;

public class NhsFhirClient(IFhirClientFactory fhirClientFactory, ILogger<NhsFhirClient> logger) : INhsFhirClient
{

    public async Task<SearchResult?> PerformSearch(SearchQuery query)
    {
        var searchParams = SearchParamsFactory.Create(query);

        try
        {
            logger.LogInformation("Searching for an Nhs patient record");

            // Search for a patient record
            var fhirClient = fhirClientFactory.CreateFhirClient();
            var patient = await fhirClient.SearchAsync<Patient>(searchParams);

            if (patient == null)
            {
                var isMultipleMatches = fhirClient.LastBodyAsResource is OperationOutcome outcome &&
                    outcome.Issue.Count > 0 &&
                    outcome.Issue[0].Code == OperationOutcome.IssueType.MultipleMatches;

                if (isMultipleMatches)
                {
                    return SearchResult.MultiMatched();
                }

                logger.LogInformation("multiple patient records found");
                return SearchResult.Error("Error occurred while parsing Nhs Digital FHIR API search response");
            }

            logger.LogInformation("{EntryCount} patient record(s) found", patient.Entry.Count);
            switch (patient.Entry.Count)
            {
                case 0:
                    return SearchResult.Unmatched();
                case 1:
                    LogInputAndPdsDifferences(query, (Patient)patient.Entry[0].Resource);

                    logger.LogInformation("Nhs patient record confidence score {Score}", patient.Entry[0].Search.Score);

                    return SearchResult.Match(patient.Entry[0].Resource.Id, patient.Entry[0].Search.Score);
                default:
                    return SearchResult.Error("Error occurred while parsing Nhs Digital FHIR API search response, more than 1 entry found");
            }
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
            var fhirClient = fhirClientFactory.CreateFhirClient();

            var data = await fhirClient.ReadAsync<Patient>(ResourceIdentity.Build("Patient", nhsId));

            return new DemographicResult() { Result = data };
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

    private void LogInputAndPdsDifferences(SearchQuery query, Patient patient)
    {
        var differentFields = FieldComparerService.ComparePatientFields(query, patient);

        logger.LogInformation(
            "[PDS_DATA_DIFF] NotUsed: {MissingFields}, Different: {DifferentFields}",
            JsonSerializer.Serialize(query.EmptyFields()),
            JsonSerializer.Serialize(differentFields)
        );
    }
}