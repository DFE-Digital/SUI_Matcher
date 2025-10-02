using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Utility;

using Shared.Endpoint;
using Shared.Models;
using Shared.Util;

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
                    logger.LogInformation("multiple patient records found");
                    return SearchResult.MultiMatched();
                }

                return SearchResult.Error("Error occurred while parsing Nhs Digital FHIR API search response");
            }

            logger.LogInformation("{EntryCount} patient record(s) found", patient.Entry.Count);
            switch (patient.Entry.Count)
            {
                case 0:
                    return SearchResult.Unmatched();
                case 1:
                    if (patient.Entry[0].Resource is Patient patientObj)
                    {
                        LogInputAndPdsDifferences(query, patientObj);
                    }

                    logger.LogInformation("Nhs patient record confidence score {Score}", patient.Entry[0].Search?.Score);

                    return SearchResult.Match(patient.Entry[0].Resource?.Id ?? string.Empty, patient.Entry[0].Search?.Score);
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
            if (data == null)
            {
                logger.LogInformation("Patient record not found for Nhs number");
                return DemographicResult();
            }

            logger.LogInformation("Patient record found for Nhs number");
            return new DemographicResult
            {
                Result = new NhsPerson
                {
                    NhsNumber = data.Id ?? String.Empty,
                    AddressPostalCodes = data.Address.Where(s => s.Period?.End == null && s.PostalCode != null).Select(s => s.PostalCode).OfType<string>().ToArray(),
                    Gender = data.Gender.GetLiteral(),
                    BirthDate = data.BirthDate.ToDateOnly([Constants.DateFormat, Constants.DateAltFormat, Constants.DateAltFormatBritish]),
                    Emails = data.Telecom
                     .Where(s => s.System is ContactPoint.ContactPointSystem.Email && s.Period?.End is null).Select(s => s.Value).OfType<string>().ToArray(),
                    PhoneNumbers = data.Telecom
                     .Where(s => s.System is ContactPoint.ContactPointSystem.Phone
                         or ContactPoint.ContactPointSystem.Sms && s.Period?.End is null).Select(s => s.Value).OfType<string>().ToArray(),
                    FamilyNames = data.Name.Where(s => s.Period?.End is null).Select(s => s.Family).OfType<string>().ToArray(),
                    GivenNames = data.Name.Where(s => s.Period?.End is null).SelectMany(s => s.Given).OfType<string>().ToArray(),
                },
                Status = Status.Success
            };
        }
        catch (Exception ex)
        {
            var status = Status.Error;
            var fhirError = string.Empty;
            if (ex is FhirOperationException { Outcome: not null } fex && fex.Outcome.Issue.Count > 0 && fex.Outcome?.Issue[0].Details?.Coding.Count > 0)
            {
                Coding coding = fex.Outcome.Issue[0].Details!.Coding[0];
                fhirError = $" - {coding.Display}";
                if (coding.Code is "INVALID_NHS_NUMBER" or "INVALID_RESOURCE_ID")
                {
                    status = Status.InvalidNhsNumber;
                }

                if (coding.Code is "PATIENT_NOT_FOUND")
                {
                    status = Status.PatientNotFound;
                }
            }
            logger.LogError(ex, "Error occurred while performing Nhs Digital FHIR API search by NHS ID{FhirError}", fhirError);
            return DemographicResult(fhirError, status);
        }
    }

    private static DemographicResult DemographicResult(string fhirError = "", Status status = Status.Error)
    {
        return new DemographicResult
        {
            ErrorMessage = "Error occurred while performing Nhs Digital FHIR API search by NHS ID" + fhirError,
            Status = status
        };
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