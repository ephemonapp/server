using Newtonsoft.Json;

namespace Ephemon.Models.Calls.Infrastructure;

public interface ICallData
{
    [JsonProperty("a", Required = Required.Always, Order = 0)]
    public string PublicKey { get; init; }

    [JsonIgnore]
    public long ServerUnixTimeMilliseconds { get; set; }
}