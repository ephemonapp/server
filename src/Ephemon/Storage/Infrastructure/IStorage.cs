namespace Ephemon.Storage.Infrastructure;

public interface IStorage<in TKey, TValue>
{
    Task<bool> UpsertAsync(
        TKey key,
        TValue value,
        CancellationToken cancellationToken,
        TimeSpan? expiry = null);

    Task<TValue?> ReadAsync(
        TKey key,
        CancellationToken cancellationToken);

    Task<bool> DeleteAsync(TKey key, CancellationToken cancellationToken);
}