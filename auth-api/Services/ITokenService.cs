namespace AuthApi.Services;

public interface ITokenService
{
    Task<string> GetBearerToken();
}