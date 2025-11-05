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
    ILogger<ReconciliationService> logger,
    INhsFhirClient nhsFhirClient,
    IAuditLogger auditLogger) : IReconciliationService
{
    public async Task<ReconciliationResponse> ReconcileAsync(ReconciliationRequest request)
    {
        var reconciliationId = BuildReconciliationId(request);
        var auditDetails = new Dictionary<string, string> { { "SearchId", reconciliationId } };
        await auditLogger.LogAsync(new AuditLogEntry(AuditLogEntry.AuditLogAction.Reconciliation, auditDetails));
        
        var matchingResponse = await matchingService.SearchAsync(request, false);

        var response = new ReconciliationResponse
        {
            MatchingResult = matchingResponse.Result
        };

        var nhsNumber = string.IsNullOrEmpty(request.NhsNumber) ? matchingResponse.Result?.NhsNumber : request.NhsNumber;

        if (string.IsNullOrEmpty(nhsNumber) || !NhsNumberValidator.Validate(nhsNumber))
        {
            var updatedResponse = UpdateResponseWithInvalidNhsNumber(nhsNumber, response);
            LogReconciliationCompleted(request, matchingResponse, updatedResponse);
            return updatedResponse;
        }

        var reconResponse = await PerformReconciliation(request, nhsNumber, matchingResponse, response);
        LogReconciliationCompleted(request, matchingResponse, reconResponse);
        return reconResponse;
    }
    
    private async Task<ReconciliationResponse> PerformReconciliation(ReconciliationRequest request,
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

        
        var differences = BuildDifferenceList(request, data.Result, matchingResponse, nhsNumber);
        var differenceString = BuildDifferences(differences);

        ReconciliationStatus status;
        if (differences.Any(x => x.FieldName == nameof(request.NhsNumber)))
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

        return response;
    }
    
    private static string GetAgeGroup(DateOnly? birthDate) =>
        !birthDate.HasValue ? "Unknown" : PersonSpecificationUtils.GetAgeGroup(birthDate.Value);
    
    private static ReconciliationResponse UpdateResponseWithInvalidNhsNumber(string? nhsNumber, ReconciliationResponse response)
    {
        if (string.IsNullOrEmpty(nhsNumber))
        {
            response.Status = ReconciliationStatus.MissingNhsNumber;
            response.Errors = ["Missing Nhs Number"];
        }
        else if (!NhsNumberValidator.Validate(nhsNumber))
        {
            response.Status = ReconciliationStatus.InvalidNhsNumber;
            response.Errors = ["The NHS Number was not valid"];
        }
        
        return response;
    }

    private void LogReconciliationCompleted(ReconciliationRequest request, PersonMatchResponse personMatchResponse, ReconciliationResponse reconciliationResponse)
    {
        var ageGroup = GetAgeGroup(reconciliationResponse.Person?.BirthDate);
        logger.LogInformation(
            "[RECONCILIATION_COMPLETED] AgeGroup: {AgeGroup}, Gender: {Gender}, Postcode: {Postcode}, Differences: {Differences}, Status: {Status}, Matching Status: {MatchingStatus}, ProcessStage: {Stage}",
            ageGroup,
            request.Gender ?? "Unknown",
            request.AddressPostalCode ?? "Unknown",
            JsonConvert.SerializeObject(reconciliationResponse.DifferenceString),
            reconciliationResponse.Status,
            personMatchResponse.Result?.MatchStatus,
            personMatchResponse.Result?.ProcessStage
        );
    }

    private static string BuildReconciliationId(ReconciliationRequest reconciliationRequest)
    {
        var data = $"{reconciliationRequest.NhsNumber}{reconciliationRequest.Given}{reconciliationRequest.Family}" +
                   $"{reconciliationRequest.BirthDate}{reconciliationRequest.Gender}{reconciliationRequest.AddressPostalCode}{reconciliationRequest.Email}{reconciliationRequest.Phone}";

        byte[] bytes = Encoding.ASCII.GetBytes(data);
        byte[] hashBytes = SHA256.HashData(bytes);

        StringBuilder builder = new();
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