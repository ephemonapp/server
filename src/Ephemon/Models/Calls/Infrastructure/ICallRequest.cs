using Newtonsoft.Json;

namespace Ephemon.Models.Calls.Infrastructure;

public interface ICallRequest
{
    [JsonProperty("a", Required = Required.Always, Order = 0)]
    public string Method { get; init; }

    [JsonProperty("c", Required = Required.Always, Order = 2)]
    public string Signature { get; init; }

    public ICallData GetData();

    public void SetServerUnixTimeMilliseconds(long value);
}

public interface ICallRequest<TData> : ICallRequest
    where TData : ICallData
{
    [JsonProperty("b", Required = Required.Always, Order = 1)]
    public TData Data { get; init; }
}