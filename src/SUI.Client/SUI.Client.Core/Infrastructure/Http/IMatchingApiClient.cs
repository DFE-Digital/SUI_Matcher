using Shared.Models;

namespace SUI.Client.Core.Infrastructure.Http;

public interface IMatchingApiClient
{
    Task<PersonMatchResponse?> MatchPersonAsync(
        SearchSpecification payload,
        CancellationToken cancellationToken
    );
}
