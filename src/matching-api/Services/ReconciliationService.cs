using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

using Newtonsoft.Json;

using Shared.Endpoint;
using Shared.Logging;
using Shared.Models;

namespace MatchingApi.Services;

public class ReconciliationService(
    ILogger<MatchingService> logger,
    INhsFhirClient nhsFhirClient,
    IValidationService validationService,
    IAuditLogger auditLogger) : IReconciliationService
{
    public async Task<ReconciliationResponse> ReconcileAsync(ReconciliationRequest reconciliationRequest)
    {
        if (reconciliationRequest.NhsNumber == null)
        {
            logger.LogError("Reconcile request missing Nhs Number");
            return new ReconciliationResponse { Errors = ["Missing Nhs Number"] };
        }

        var result = await nhsFhirClient.PerformSearchByNhsId(reconciliationRequest.NhsNumber);
        if (result.Result == null || !string.IsNullOrEmpty(result.ErrorMessage))
        {
            return new ReconciliationResponse
            {
                Errors = [result.ErrorMessage ?? "Unknown error"]
            };
        }
        return new ReconciliationResponse
        {
            Result = result.Result,
            Differences = BuildDifferenceList(reconciliationRequest, result.Result),
        };
    }

    private List<Difference> BuildDifferenceList(ReconciliationRequest request, NhsPerson result)
    {
        var differences = new List<Difference>();
        if (request.NhsNumber != result.NhsNumber)
        {
            differences.Add(new Difference { FieldName = "NhsNumber", Local = request.NhsNumber, Nhs = result.NhsNumber });
        }

        if (request.BirthDate != result.BirthDate)
        {
            differences.Add(new Difference
            {
                FieldName = "BirthDate",
                Local = request.BirthDate?.ToString("yyyy-MM-dd"),
                Nhs = result.BirthDate?.ToString("yyyy-MM-dd"),
            });
        }

        if (!result.Emails.Contains(request.Email))
        {
            differences.Add(new Difference { FieldName = "Email", Local = request.Email, Nhs = string.Join(", ", result.Emails) });
        }

        if (!result.PhoneNumbers.Contains(request.Phone))
        {
            differences.Add(new Difference { FieldName = "Phone", Local = request.Phone, Nhs = string.Join(", ", result.PhoneNumbers) });
        }

        if (!result.GivenNames.Contains(request.Given))
        {
            differences.Add(new Difference { FieldName = "Given", Local = request.Given, Nhs = string.Join(", ", result.GivenNames) });
        }

        if (!result.FamilyNames.Contains(request.Family))
        {
            differences.Add(new Difference { FieldName = "Family", Local = request.Family, Nhs = string.Join(", ", result.FamilyNames) });
        }

        if (request.Gender != result.Gender)
        {
            differences.Add(new Difference { FieldName = "Gender", Local = request.Gender, Nhs = result.Gender });
        }

        if (!result.AddressPostalCodes.Contains(request.AddressPostalCode))
        {
            differences.Add(new Difference { FieldName = "AddressPostalCode", Local = request.AddressPostalCode, Nhs = string.Join(", ", result.AddressPostalCodes) });
        }

        return differences;
    }
}