namespace Ephemon.Services.DbDateTime;

public interface IDbDateTimeProvider
{
    public Task<long> GetServerTimeAsync();
}