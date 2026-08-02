using Newtonsoft.Json;
using Ephemon.Data;
using Ephemon.Models.Calls.Infrastructure;

namespace Ephemon.Models.Calls.Update;

internal sealed record UpdateCallData : CallData
{
    [JsonProperty("b", NullValueHandling = NullValueHandling.Ignore, Order = 1)]
    public Subscription? Subscription { get; init; }
}