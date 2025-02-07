namespace AuthApi.Models;

public static class NhsDigitalKeyConstants
{
    public const string ClientId = "nhs-digital.client-id";
    public const string PrivateKey = "nhs-digital.private-key";
    public const string Kid = "nhs-digital.kid";
    public const string AccountToken = "nhsDigitalAccessToken";
    public const int AccountTokenExpiresInMinutes = 5;
    public const int AccountTokenRedisStorageExpiresInMinutes = 4;
}