using Shared.Models;

namespace SUI.Client.Core.Application.Interfaces;

public interface IMatchingApiClient
{
    Task<PersonMatchResponse?> MatchPersonAsync(
        SearchSpecification payload,
        CancellationToken cancellationToken
    );

    Task<ReconciliationResponse?> ReconcilePersonAsync(
        ReconciliationRequest payload,
        CancellationToken cancellationToken
    );
}
