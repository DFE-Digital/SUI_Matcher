using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

using Newtonsoft.Json;

using Shared.Endpoint;
using Shared.Logging;
using Shared.Models;
using Shared.Util;

namespace MatchingApi.Services;

public class ReconciliationService(
    IMatchingService matchingService,
    ILogger<MatchingService> logger,
    INhsFhirClient nhsFhirClient,
    IAuditLogger auditLogger) : IReconciliationService
{
    public async Task<ReconciliationResponse> ReconcileAsync(ReconciliationRequest reconciliationRequest)
    {
        var reconciliationId = BuildReconciliationId(reconciliationRequest);
        var auditDetails = new Dictionary<string, string> { { "SearchId", reconciliationId } };
        await auditLogger.LogAsync(new AuditLogEntry(AuditLogEntry.AuditLogAction.Reconciliation, auditDetails));

        var response = new ReconciliationResponse();

        var matchingResponse = await matchingService.SearchAsync(reconciliationRequest, false);
        response.MatchingResult = matchingResponse.Result;

        var nhsNumber = string.IsNullOrEmpty(reconciliationRequest.NhsNumber) ? matchingResponse.Result?.NhsNumber : reconciliationRequest.NhsNumber;

        if (string.IsNullOrEmpty(nhsNumber) || !NhsNumberValidator.Validate(nhsNumber))
        {
            var requestAgeGroup = reconciliationRequest.BirthDate.HasValue
                ? PersonSpecificationUtils.GetAgeGroup(reconciliationRequest.BirthDate.Value)
                : "Unknown";
            
            var status = string.IsNullOrEmpty(nhsNumber)
                ? ReconciliationStatus.MissingNhsNumber
                : ReconciliationStatus.InvalidNhsNumber;
            var error = string.IsNullOrEmpty(nhsNumber)
                ? "Missing Nhs Number"
                : "The NHS Number was not valid";

            response.Status = status;
            response.Errors = [error];
            LogReconciliationCompleted(reconciliationRequest, matchingResponse, response.Status, requestAgeGroup, string.Empty);
            return response;
        }

        return await PerformReconciliation(reconciliationRequest, nhsNumber, matchingResponse, response);
    }

    private async Task<ReconciliationResponse> PerformReconciliation(ReconciliationRequest reconciliationRequest,
        string nhsNumber,
        PersonMatchResponse matchingResponse, ReconciliationResponse response)
    {
        var data = await nhsFhirClient.PerformSearchByNhsId(nhsNumber);

        if (data.Status == Status.InvalidNhsNumber)
        {
            response.Status = ReconciliationStatus.InvalidNhsNumber;
            response.Errors = [data.ErrorMessage ?? "Unknown error"];
        }

        if (data.Status == Status.PatientNotFound)
        {
            response.Status = ReconciliationStatus.PatientNotFound;
            response.Errors = [data.ErrorMessage ?? "Unknown error"];
        }

        if (data.Status == Status.Error || data.Result == null)
        {
            response.Status = ReconciliationStatus.Error;
            response.Errors = [data.ErrorMessage ?? "Unknown error"];
        }

        var ageGroup = data.Result != null && data.Result.BirthDate.HasValue
            ? PersonSpecificationUtils.GetAgeGroup(data.Result.BirthDate.Value)
            : "Unknown";

        var differences = BuildDifferenceList(reconciliationRequest, data.Result, matchingResponse, nhsNumber);
        var differenceString = BuildDifferences(differences);

        ReconciliationStatus status;
        if (differences.Any(x => x.FieldName == nameof(reconciliationRequest.NhsNumber)))
        {
            status = ReconciliationStatus.SupersededNhsNumber;
        }
        else if (differences.Count == 0)
        {
            status = ReconciliationStatus.NoDifferences;
        }
        else
        {
            status = ReconciliationStatus.Differences;
        }

        response.Person = data.Result;
        response.Differences = differences;
        response.DifferenceString = differenceString;
        response.Status = status;

        LogReconciliationCompleted(reconciliationRequest, matchingResponse, status, ageGroup, differenceString);

        return response;
    }

    private void LogReconciliationCompleted(ReconciliationRequest request, PersonMatchResponse response, ReconciliationStatus status, string ageGroup, string differenceString)
    {
        logger.LogInformation(
            "[RECONCILIATION_COMPLETED] AgeGroup: {AgeGroup}, Gender: {Gender}, Postcode: {Postcode}, Differences: {Differences}, Status: {Status}, Matching Status: {MatchingStatus}, ProcessStage: {Stage}",
            ageGroup,
            request.Gender ?? "Unknown",
            request.AddressPostalCode ?? "Unknown",
            JsonConvert.SerializeObject(differenceString),
            status,
            response.Result?.MatchStatus,
            response.Result?.ProcessStage
        );
    }

    private static string BuildReconciliationId(ReconciliationRequest reconciliationRequest)
    {
        var data = $"{reconciliationRequest.NhsNumber}{reconciliationRequest.Given}{reconciliationRequest.Family}" +
                   $"{reconciliationRequest.BirthDate}{reconciliationRequest.Gender}{reconciliationRequest.AddressPostalCode}{reconciliationRequest.Email}{reconciliationRequest.Phone}";

        byte[] bytes = Encoding.ASCII.GetBytes(data);
        byte[] hashBytes = SHA256.HashData(bytes);

        StringBuilder builder = new StringBuilder();
        foreach (var t in hashBytes)
        {
            builder.Append(t.ToString("x2"));
        }

        var hash = builder.ToString();

        Activity.Current?.SetBaggage("ReconciliationId", hash);

        return hash;
    }

    private static string BuildDifferences(List<Difference>? differences)
    {
        var sb = new StringBuilder();
        if (differences == null)
        {
            return sb.ToString();
        }

        foreach (var difference in differences)
        {
            if (string.IsNullOrEmpty(difference.Local) && string.IsNullOrEmpty(difference.Nhs))
            {
                sb.Append($"{difference.FieldName}:Both");
            }
            else if (string.IsNullOrEmpty(difference.Local))
            {
                sb.Append($"{difference.FieldName}:LA");
            }
            else if (string.IsNullOrEmpty(difference.Nhs))
            {
                sb.Append($"{difference.FieldName}:NHS");
            }
            else
            {
                sb.Append($"{difference.FieldName}");
            }

            sb.Append(" - ");
        }

        return sb.ToString().EndsWith(" - ") ? sb.ToString(0, sb.Length - 3) : sb.ToString();
    }

    private static List<Difference> BuildDifferenceList(ReconciliationRequest request, NhsPerson? result, PersonMatchResponse matchResult, string nhsNumber)
    {
        var differences = new List<Difference>();
        if (result == null)
        {
            return differences;
        }

        AddDifferenceIfUnequal(differences, nameof(request.NhsNumber), nhsNumber, result.NhsNumber);
        AddDifferenceIfUnequal(differences, nameof(request.BirthDate), request.BirthDate, result.BirthDate);
        AddDifferenceIfUnequal(differences, nameof(request.Gender), request.Gender, result.Gender);
        AddDifferenceIfUnequal(differences, nameof(request.Given), request.Given, result.GivenNames);
        AddDifferenceIfUnequal(differences, nameof(request.Family), request.Family, result.FamilyNames);
        AddDifferenceIfUnequal(differences, nameof(request.Email), request.Email, result.Emails);
        AddDifferenceIfUnequal(differences, nameof(request.Phone), request.Phone, result.PhoneNumbers);
        AddDifferenceIfUnequal(differences, nameof(request.AddressPostalCode), request.AddressPostalCode, result.AddressPostalCodes);
        AddDifferenceIfUnequal(differences, "MatchingNhsNumber", request.NhsNumber, matchResult.Result?.NhsNumber);

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