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
    ILogger<MatchingService> logger,
    INhsFhirClient nhsFhirClient,
    IAuditLogger auditLogger) : IReconciliationService
{
    public async Task<ReconciliationResponse> ReconcileAsync(ReconciliationRequest reconciliationRequest)
    {
        var reconciliationId = BuildReconciliationId(reconciliationRequest);
        var auditDetails = new Dictionary<string, string>
        {
            { "SearchId", reconciliationId }
        };
        await auditLogger.LogAsync(new AuditLogEntry(AuditLogEntry.AuditLogAction.Reconciliation, auditDetails));
        if (reconciliationRequest.NhsNumber == null)
        {
            logger.LogError("Reconcile request missing Nhs Number");
            return new ReconciliationResponse { Errors = ["Missing Nhs Number"] };
        }

        var data = await nhsFhirClient.PerformSearchByNhsId(reconciliationRequest.NhsNumber);
        if (data.Result == null || !string.IsNullOrEmpty(data.ErrorMessage))
        {
            return new ReconciliationResponse
            {
                Status = ReconciliationStatus.Error,
                Errors = [data.ErrorMessage ?? "Unknown error"]
            };
        }
        var ageGroup = !string.IsNullOrEmpty(data.Result.BirthDate)
            ? PersonSpecificationUtils.GetAgeGroup(data.Result.BirthDate.ToDateOnly([Constants.DateFormat, Constants.DateAltFormat, Constants.DateAltFormatBritish])!.Value)
            : "Unknown";

        var differences = BuildDifferenceList(reconciliationRequest, data.Result);

        var status = ReconciliationStatus.Error;
        if (differences.Any(x => x.FieldName == nameof(reconciliationRequest.NhsNumber)))
        {
            status = ReconciliationStatus.SupersededNhsNumber;
        }
        else if (differences.Count == 0)
        {
            status = ReconciliationStatus.NoDifferences;
        }
        else if (differences.Count == 1)
        {
            status = ReconciliationStatus.OneDifference;
        }
        else if (differences.Count > 1)
        {
            status = ReconciliationStatus.ManyDifferences;
        }
        logger.LogInformation(
            "[RECONCILIATION_COMPLETED] AgeGroup: {AgeGroup}, Gender: {Gender}, Postcode: {Postcode}, Differences: {Differences}, Status {Status}",
            ageGroup,
            reconciliationRequest.Gender ?? "Unknown",
            reconciliationRequest.AddressPostalCode ?? "Unknown",
            JsonConvert.SerializeObject(differences.Select(x => x.FieldName)),
            status
        );

        return new ReconciliationResponse
        {
            Person = data.Result,
            Differences = differences,
            Status = status,
        };
    }

    private static string BuildReconciliationId(ReconciliationRequest reconciliationRequest)
    {
        var data = $"{reconciliationRequest.NhsNumber}{reconciliationRequest.Given}{reconciliationRequest.Family}" +
                   $"{reconciliationRequest.BirthDate}{reconciliationRequest.Gender}{reconciliationRequest.AddressPostalCode}{reconciliationRequest.Email}{reconciliationRequest.Phone}";

        byte[] bytes = Encoding.ASCII.GetBytes(data);
        byte[] hashBytes = SHA256.HashData(bytes);

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < hashBytes.Length; i++)
        {
            builder.Append(hashBytes[i].ToString("x2"));
        }

        var hash = builder.ToString();

        Activity.Current?.SetBaggage("ReconciliationId", hash);

        return hash;
    }

    private static List<Difference> BuildDifferenceList(ReconciliationRequest request, NhsPerson result)
    {
        var differences = new List<Difference>();
        if (request.NhsNumber != result.NhsNumber)
        {
            differences.Add(new Difference { FieldName = nameof(request.NhsNumber), Local = request.NhsNumber, Nhs = result.NhsNumber });
        }

        if (request.BirthDate != result.BirthDate.ToDateOnly([Constants.DateFormat, Constants.DateAltFormat, Constants.DateAltFormatBritish]))
        {
            differences.Add(new Difference
            {
                FieldName = nameof(request.BirthDate),
                Local = request.BirthDate?.ToString("yyyy-MM-dd"),
                Nhs = result.BirthDate,
            });
        }

        if (result.Emails.Length == 0 && !String.IsNullOrEmpty(request.Email) || result.Emails.Length > 0 && !result.Emails.Contains(request.Email, StringComparer.OrdinalIgnoreCase))
        {
            differences.Add(new Difference { FieldName = nameof(request.Email), Local = request.Email, Nhs = string.Join(", ", result.Emails) });
        }

        if (result.PhoneNumbers.Length == 0 && !String.IsNullOrEmpty(request.Phone) || result.PhoneNumbers.Length > 0 && !result.PhoneNumbers.Contains(request.Phone, StringComparer.OrdinalIgnoreCase))
        {
            differences.Add(new Difference { FieldName = nameof(request.Phone), Local = request.Phone, Nhs = string.Join(", ", result.PhoneNumbers) });
        }

        if (result.GivenNames.Length == 0 && !String.IsNullOrEmpty(request.Given) || result.GivenNames.Length > 0 && !result.GivenNames.Contains(request.Given, StringComparer.OrdinalIgnoreCase))
        {
            differences.Add(new Difference { FieldName = nameof(request.Given), Local = request.Given, Nhs = string.Join(", ", result.GivenNames) });
        }

        if (result.FamilyNames.Length == 0 && !String.IsNullOrEmpty(request.Family) || result.FamilyNames.Length > 0 && !result.FamilyNames.Contains(request.Family, StringComparer.OrdinalIgnoreCase))
        {
            differences.Add(new Difference { FieldName = nameof(request.Family), Local = request.Family, Nhs = string.Join(", ", result.FamilyNames) });
        }

        if (!String.Equals(request.Gender, result.Gender, StringComparison.OrdinalIgnoreCase))
        {
            differences.Add(new Difference { FieldName = nameof(request.Gender), Local = request.Gender, Nhs = result.Gender });
        }

        if (result.AddressPostalCodes.Length == 0 && !String.IsNullOrEmpty(request.AddressPostalCode) || result.AddressPostalCodes.Length > 0 && !result.AddressPostalCodes.Contains(request.AddressPostalCode, StringComparer.OrdinalIgnoreCase))
        {
            differences.Add(new Difference { FieldName = nameof(request.AddressPostalCode), Local = request.AddressPostalCode, Nhs = string.Join(", ", result.AddressPostalCodes) });
        }

        return differences;
    }
}