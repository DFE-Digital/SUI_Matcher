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
        if (string.IsNullOrEmpty(reconciliationRequest.NhsNumber))
        {
            logger.LogError("Reconcile request missing Nhs Number");
            return new ReconciliationResponse
            {
                Status = ReconciliationStatus.MissingNhsNumber,
                Errors = ["Missing Nhs Number"]
            };
        }

        if (!NhsNumberValidator.Validate(reconciliationRequest.NhsNumber))
        {
            logger.LogError("NHS Number Validation failed");
            return new ReconciliationResponse
            {
                Status = ReconciliationStatus.InvalidNhsNumber,
                Errors = ["The NHS Number was not valid"]
            };
        }

        var matchingResponse = await matchingService.SearchAsync(reconciliationRequest, false);

        var data = await nhsFhirClient.PerformSearchByNhsId(reconciliationRequest.NhsNumber);
        if (data.Status == Status.InvalidNhsNumber)
        {
            return new ReconciliationResponse
            {
                Status = ReconciliationStatus.InvalidNhsNumber,
                Errors = [data.ErrorMessage ?? "Unknown error"]
            };
        }

        if (data.Status == Status.PatientNotFound)
        {
            return new ReconciliationResponse
            {
                Status = ReconciliationStatus.PatientNotFound,
                Errors = [data.ErrorMessage ?? "Unknown error"]
            };
        }

        if (data.Status == Status.Error || data.Result == null)
        {
            return new ReconciliationResponse
            {
                Status = ReconciliationStatus.Error,
                Errors = [data.ErrorMessage ?? "Unknown error"]
            };
        }

        var ageGroup = data.Result.BirthDate.HasValue
            ? PersonSpecificationUtils.GetAgeGroup(data.Result.BirthDate.Value)
            : "Unknown";

        var differences = BuildDifferenceList(reconciliationRequest, data.Result, matchingResponse);
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

        logger.LogInformation(
            "[RECONCILIATION_COMPLETED] AgeGroup: {AgeGroup}, Gender: {Gender}, Postcode: {Postcode}, Differences: {Differences}, Status {Status}, Matching Status: {MatchingStatus}",
            ageGroup,
            reconciliationRequest.Gender ?? "Unknown",
            reconciliationRequest.AddressPostalCode ?? "Unknown",
            JsonConvert.SerializeObject(differenceString),
            status,
            matchingResponse.Result?.MatchStatus
        );

        return new ReconciliationResponse
        {
            Person = data.Result,
            Differences = differences,
            DifferenceString = differenceString,
            Status = status,
            MatchingResult = matchingResponse.Result
        };
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

    private static List<Difference> BuildDifferenceList(ReconciliationRequest request, NhsPerson result, PersonMatchResponse matchResult)
    {
        var differences = new List<Difference>();

        // The main method remains clean, with all logic in the helpers
        AddDifferenceIfUnequal(differences, nameof(request.NhsNumber), request.NhsNumber, result.NhsNumber);
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