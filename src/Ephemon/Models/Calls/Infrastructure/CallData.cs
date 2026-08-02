namespace Ephemon.Models.Calls.Infrastructure;

internal abstract record CallData : ICallData
{
    public string PublicKey { get; init; } = null!;
    public long ServerUnixTimeMilliseconds { get; set; }
}