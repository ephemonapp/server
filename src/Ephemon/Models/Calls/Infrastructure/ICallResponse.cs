using Newtonsoft.Json;

namespace Ephemon.Models.Calls.Infrastructure;

public interface ICallResponse
{
    [JsonProperty("ok", Required = Required.Always, Order = 0)]
    public bool Ok { get; init; }

    [JsonProperty("timestamp", Required = Required.Always, Order = 1)]
    public long Timestamp { get; init; }

    [JsonProperty("reason", NullValueHandling = NullValueHandling.Ignore, Order = 2)]
    public string? Reason { get; init; }

    [JsonProperty("errors", NullValueHandling = NullValueHandling.Ignore, Order = 3)]
    public IReadOnlyCollection<string>? Errors { get; init; }
}