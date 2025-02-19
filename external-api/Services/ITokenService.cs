using ExternalApi.Models;

namespace ExternalApi.Services;

public interface ITokenService
{
    Task<string> GetBearerToken();

    Task Initialise();
}