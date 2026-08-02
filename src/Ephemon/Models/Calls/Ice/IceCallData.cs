using Newtonsoft.Json;
using Ephemon.Models.Calls.Infrastructure;

namespace Ephemon.Models.Calls.Ice;

internal sealed record IceCallData : EncryptedCallData
{
    [JsonProperty("f", Required = Required.Always, Order = 5)]
    public IceDirection Direction { get; init; } = IceDirection.Unknown;
}