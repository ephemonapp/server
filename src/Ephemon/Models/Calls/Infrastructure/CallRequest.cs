namespace Ephemon.Models.Calls.Infrastructure;

internal abstract record CallRequest : ICallRequest
{
    public string Method { get; init; } = null!;
    public string Signature { get; init; } = null!;
    public abstract ICallData GetData();
    public abstract void SetServerUnixTimeMilliseconds(long value);
}

internal abstract record CallRequest<T> : CallRequest, ICallRequest<T>
    where T : class, ICallData
{
    public T Data { get; init; } = null!;

    public sealed override ICallData GetData()
    {
        return Data;
    }

    public sealed override void SetServerUnixTimeMilliseconds(long value)
    {
        if (Data.ServerUnixTimeMilliseconds != 0)
            throw new InvalidOperationException("ServerUnixTimeMilliseconds has already been set");

        Data.ServerUnixTimeMilliseconds = value;
    }
}