using StackExchange.Redis;
using Ephemon.Data;
using Ephemon.Services.Serializers.Infrastructure;
using Ephemon.Storage.Redis.Infrastructure;

namespace Ephemon.Storage.Redis;

internal sealed class SubscriptionRedisStorage(
    IConnectionMultiplexer connectionMultiplexer,
    IJsonSerializer<Subscription> serializer) :
    RedisStorage<Subscription>(connectionMultiplexer, serializer),
    ISubscriptionStorage
{
    protected override string KeyPrefix => "subscription";
}