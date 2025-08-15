using Shared.Models;

namespace Shared.Endpoint;

public interface IReconciliationService
{
    Task<ReconciliationResponse> ReconcileAsync(ReconciliationRequest reconciliationRequest);
}