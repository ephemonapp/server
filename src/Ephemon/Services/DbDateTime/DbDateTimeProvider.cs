using StackExchange.Redis;

namespace Ephemon.Services.DbDateTime;

internal sealed class DbDateTimeProvider(IConnectionMultiplexer connectionMultiplexer) : IDbDateTimeProvider
{
    public async Task<long> GetServerTimeAsync()
    {
        var database = connectionMultiplexer.GetDatabase(0);
        var result = await database.ExecuteAsync("TIME");
        var timeArray = (RedisResult[])result!;
        var unixSeconds = (long)timeArray[0];
        var microSeconds = (long)timeArray[1];

        return unixSeconds * 1000 + microSeconds / 1000;
    }
}