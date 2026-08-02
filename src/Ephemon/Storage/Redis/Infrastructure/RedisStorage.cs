using StackExchange.Redis;
using Ephemon.Services.Serializers.Infrastructure;
using Ephemon.Storage.Infrastructure;

namespace Ephemon.Storage.Redis.Infrastructure;

internal abstract class RedisStorage<TValue> : IStorage<string, TValue>
{
    private readonly IDatabase _database;
    private readonly IJsonSerializer<TValue> _serializer;

    protected RedisStorage(
        IConnectionMultiplexer connectionMultiplexer,
        IJsonSerializer<TValue> serializer)
    {
        // ReSharper disable once VirtualMemberCallInConstructor
        _database = connectionMultiplexer.GetDatabase(DatabaseId);
        _serializer = serializer;
    }

    protected virtual int DatabaseId => 0;

    protected abstract string KeyPrefix { get; }

    public async Task<bool> UpsertAsync(
        string key,
        TValue value,
        CancellationToken cancellationToken,
        TimeSpan? expiry = null)
    {
        return await UpsertInternalAsync(key, value, cancellationToken, expiry);
    }

    public async Task<TValue?> ReadAsync(
        string publicKey,
        CancellationToken cancellationToken)
    {
        return await ReadInternalAsync(publicKey, cancellationToken);
    }

    public async Task<bool> DeleteAsync(
        string key,
        CancellationToken cancellationToken)
    {
        return await DeleteInternalAsync(key, cancellationToken);
    }

    protected virtual async Task<bool> UpsertInternalAsync(
        string key,
        TValue value,
        CancellationToken cancellationToken,
        TimeSpan? expiry = null)
    {
        return await _database.StringSetAsync(
            GetKey(key),
            _serializer.Serialize(value),
            expiry ?? Expiration.Default);
    }

    protected virtual async Task<TValue?> ReadInternalAsync(
        string key,
        CancellationToken cancellationToken)
    {
        string? redisValue = await _database.StringGetAsync(GetKey(key));
        return redisValue != null
            ? _serializer.Deserialize(redisValue)
            : default;
    }

    protected virtual async Task<bool> DeleteInternalAsync(
        string key,
        CancellationToken cancellationToken)
    {
        return await _database.KeyDeleteAsync(GetKey(key));
    }

    private string GetKey(string key)
    {
        return $"ephemon{(string.IsNullOrWhiteSpace(KeyPrefix) ? string.Empty : $":{KeyPrefix}")}:{key}";
    }
}