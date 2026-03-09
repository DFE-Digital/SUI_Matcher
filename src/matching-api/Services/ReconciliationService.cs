using Newtonsoft.Json;

using Shared.Endpoint;
using Shared.Models;
using Shared.Util;

namespace MatchingApi.Services;

public class ReconciliationService(
    IMatchingService matchingService,
    ILogger<ReconciliationService> logger,
    INhsFhirClient nhsFhirClient) : IReconciliationService
{
    public async Task<ReconciliationResponse> ReconcileAsync(ReconciliationRequest request)
    {
        var localMissingFields = BuildLocalMissingFields(request);
        // Match the request's demographics to an NHS number
        var matchingResponse = await matchingService.SearchAsync(request, false);

        if (string.IsNullOrWhiteSpace(matchingResponse.Result?.NhsNumber))
        {
            var reconciliationResponse = new ReconciliationResponse
            {
                MatchingResult = matchingResponse.Result,
                Status = ReconciliationStatus.LocalDemographicsDidNotMatchToAnNhsNumber,
                Errors = ["Local demographics did not match to an NHS number"],
                MissingLocalFields = localMissingFields
            };
            LogReconciliationCompleted(request, matchingResponse, reconciliationResponse);
            return reconciliationResponse;
        }

        // Fetch the NHS demographics for the matched NHS number
        var matchedNhsNumberDemographics =
            await nhsFhirClient.PerformSearchByNhsId(matchingResponse.Result?.NhsNumber);

        if (matchedNhsNumberDemographics.Status == Status.Error || matchedNhsNumberDemographics.Result == null)
        {
            var reconciliationResponse = new ReconciliationResponse
            {
                MatchingResult = matchingResponse.Result,
                // Generic error, since we'd expect a matched number to return demographics
                Status = ReconciliationStatus.Error,
                Errors = [matchedNhsNumberDemographics.ErrorMessage ?? "Unknown error"],
                MissingLocalFields = localMissingFields
            };
            LogReconciliationCompleted(request, matchingResponse, reconciliationResponse);
            return reconciliationResponse;
        }

        // Compare matched NHS number's demographics to the request's demographics
        var differences = BuildDifferenceList(request, matchedNhsNumberDemographics.Result);
        var differenceFields = differences
            .Where(d => !string.IsNullOrEmpty(d.Local) && !string.IsNullOrEmpty(d.Nhs) && !d.Local.Equals(d.Nhs, StringComparison.OrdinalIgnoreCase))
            .Select(d => d.FieldName)
            .ToList();
        
        // Can only get these if we get NHS demographics, so we build these after that step
        var nhsMissingFields = differences.Where(d => string.IsNullOrEmpty(d.Nhs)).Select(d => d.FieldName).ToList();

        // Prepare response, with initial status on whether differences have occurred
        var reconResponse = new ReconciliationResponse
        {
            MatchingResult = matchingResponse.Result,
            Person = matchedNhsNumberDemographics.Result,
            Differences = differences,
            Status = differences.Count == 0
                ? ReconciliationStatus.NoDifferences
                : ReconciliationStatus.Differences,
            DifferenceFields = differenceFields,
            MissingLocalFields = localMissingFields,
            MissingNhsFields = nhsMissingFields
        };

        // Return early if the NHS number definitely can't be superseded
        var nhsNumberCantBeSuperseded = string.IsNullOrEmpty(request.NhsNumber)
                                        || request.NhsNumber == matchedNhsNumberDemographics.Result.NhsNumber;
        if (nhsNumberCantBeSuperseded)
        {
            LogReconciliationCompleted(request, matchingResponse, reconResponse);
            return reconResponse;
        }

        // Otherwise, fetch demographics of the request's NHS number to check if it is superseded

        // Check if the request's NHS number is valid. If not, return with that status
        if (!NhsNumberValidator.Validate(request.NhsNumber))
        {
            reconResponse.Status = ReconciliationStatus.LocalNhsNumberIsNotValid;
            LogReconciliationCompleted(request, matchingResponse, reconResponse);
            return reconResponse;
        }

        // Request demographics 
        var requestNhsNumberDemographics =
            await nhsFhirClient.PerformSearchByNhsId(request.NhsNumber);

        // If no success when fetching the NHS number from the request, return the result with these as errors
        if (requestNhsNumberDemographics.Status != Status.Success)
        {
            reconResponse.Status = requestNhsNumberDemographics.Status == Status.PatientNotFound
                ? ReconciliationStatus.LocalNhsNumberIsNotFoundInNhs
                : ReconciliationStatus.Error;
            reconResponse.Errors = [requestNhsNumberDemographics.ErrorMessage ?? "Unknown error"];
            LogReconciliationCompleted(request, matchingResponse, reconResponse);
            return reconResponse;
        }

        bool requestNhsNumberHasBeenSuperseded =
            requestNhsNumberDemographics.Result != null && request.NhsNumber != requestNhsNumberDemographics.Result.NhsNumber;
        // Since the demographics response will contain the new NHS number if the inputted
        // NHS number has been superseded.
        if (requestNhsNumberHasBeenSuperseded)
        {
            reconResponse.Status = ReconciliationStatus.LocalNhsNumberIsSuperseded;
        }

        LogReconciliationCompleted(request, matchingResponse, reconResponse);
        return reconResponse;
    }

    private static string GetAgeGroup(DateOnly? birthDate) =>
        !birthDate.HasValue ? "Unknown" : PersonSpecificationUtils.GetAgeGroup(birthDate.Value);

    private void LogReconciliationCompleted(ReconciliationRequest request, PersonMatchResponse personMatchResponse, ReconciliationResponse reconciliationResponse)
    {
        var ageGroup = GetAgeGroup(reconciliationResponse.Person?.BirthDate);
        decimal score = personMatchResponse.Result?.Score ?? 0;
        logger.LogInformation(
            "[RECONCILIATION_COMPLETED] AgeGroup: {AgeGroup}, Gender: {Gender}, Postcode: {Postcode}, Differences: {Differences}, LocaMising: {LocalMissing}, NhsMissing: {NhsMissing} Status: {Status}, Matching Status: {MatchingStatus}, ProcessStage: {Stage}, Confidence Score: {Score}",
            ageGroup,
            request.Gender ?? "Unknown",
            request.AddressPostalCode ?? "Unknown",
            JsonConvert.SerializeObject(reconciliationResponse.DifferenceFields),
            JsonConvert.SerializeObject(reconciliationResponse.MissingLocalFields),
            JsonConvert.SerializeObject(reconciliationResponse.MissingNhsFields),
            reconciliationResponse.Status,
            personMatchResponse.Result?.MatchStatus,
            personMatchResponse.Result?.ProcessStage,
            score
        );
    }
    
    private static List<string> BuildLocalMissingFields(ReconciliationRequest request)
    {
        var missingFields = new List<string>();
        
        if (string.IsNullOrEmpty(request.NhsNumber))
            missingFields.Add(nameof(request.NhsNumber));
        
        if (!request.BirthDate.HasValue)
            missingFields.Add(nameof(request.BirthDate));
        
        if (string.IsNullOrEmpty(request.Gender))
            missingFields.Add(nameof(request.Gender));
        
        if (string.IsNullOrEmpty(request.Given))
            missingFields.Add(nameof(request.Given));
        
        if (string.IsNullOrEmpty(request.Family))
            missingFields.Add(nameof(request.Family));
        
        if (string.IsNullOrEmpty(request.Email))
            missingFields.Add(nameof(request.Email));
        
        if (string.IsNullOrEmpty(request.Phone))
            missingFields.Add(nameof(request.Phone));
        
        if (string.IsNullOrEmpty(request.AddressPostalCode))
            missingFields.Add(nameof(request.AddressPostalCode));
        
        return missingFields;
    }

    private static List<Difference> BuildDifferenceList(ReconciliationRequest request, NhsPerson? result)
    {
        var differences = new List<Difference>();
        if (result == null)
        {
            return differences;
        }

        AddDifferenceIfUnequal(differences, nameof(request.NhsNumber), request.NhsNumber, result.NhsNumber);
        AddDifferenceIfUnequal(differences, nameof(request.BirthDate), request.BirthDate, result.BirthDate);
        AddDifferenceIfUnequal(differences, nameof(request.Gender), request.Gender, result.Gender);
        AddDifferenceIfUnequal(differences, nameof(request.Given), request.Given, result.GivenNames);
        AddDifferenceIfUnequal(differences, nameof(request.Family), request.Family, result.FamilyNames);
        AddDifferenceIfUnequal(differences, nameof(request.Email), request.Email, result.Emails);
        AddDifferenceIfUnequal(differences, nameof(request.Phone), request.Phone, result.PhoneNumbers);
        AddDifferenceIfUnequal(differences, nameof(request.AddressPostalCode), request.AddressPostalCode, result.AddressPostalCodes);

        return differences;
    }

    private static void AddDifferenceIfUnequal(List<Difference> diffs, string fieldName, string? local, string? nhs)
    {
        bool areTheSame = !string.IsNullOrEmpty(local)
                          && !string.IsNullOrEmpty(nhs)
                          && local.Equals(nhs, StringComparison.OrdinalIgnoreCase);

        if (!areTheSame)
        {
            diffs.Add(new Difference { FieldName = fieldName, Local = local, Nhs = nhs });
        }
    }

    private static void AddDifferenceIfUnequal(List<Difference> diffs, string fieldName, DateOnly? local, DateOnly? nhs)
    {
        bool areTheSame = local.HasValue
                          && nhs.HasValue
                          && local.Value == nhs.Value;

        if (areTheSame)
        {
            return;
        }

        const string dateFormat = "yyyy-MM-dd";
        diffs.Add(new Difference
        {
            FieldName = fieldName,
            Local = local?.ToString(dateFormat),
            Nhs = nhs?.ToString(dateFormat)
        });
    }

    private static void AddDifferenceIfUnequal(List<Difference> diffs, string fieldName, string? local, string[] nhsValues)
    {
        bool areTheSame = !string.IsNullOrEmpty(local)
                          && nhsValues.Length > 0
                          && nhsValues.Contains(local, StringComparer.OrdinalIgnoreCase);

        if (areTheSame)
        {
            return;
        }

        diffs.Add(new Difference { FieldName = fieldName, Local = local, Nhs = string.Join(", ", nhsValues) });
    }
}