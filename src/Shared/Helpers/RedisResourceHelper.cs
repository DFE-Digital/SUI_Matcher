namespace Shared.Helpers;

using Aspire.Hosting.ApplicationModel;

using StackExchange.Redis;

public static class RedisResourceHelper
{
    public static async Task<IDatabase> GetDatabase(this RedisResource res)
    {
        var redisConnectionString = await res.GetConnectionStringAsync();
        var redisConnection = ConnectionMultiplexer.Connect(redisConnectionString!);
        return await Task.FromResult(redisConnection.GetDatabase());
    }
}