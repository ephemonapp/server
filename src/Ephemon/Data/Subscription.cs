using Newtonsoft.Json;
using Ephemon.Services.Notifications;

namespace Ephemon.Data;

public record Subscription : ISubscription<SubscriptionKeys>
{
    [JsonProperty("a", Required = Required.Always, Order = 0)]
    public string Endpoint { get; init; } = null!;

    [JsonProperty("b", Order = 1)]
    public long? ExpirationTime { get; init; }

    [JsonProperty("c", Required = Required.Always, Order = 2)]
    public SubscriptionKeys Keys { get; init; } = null!;

    public ISubscriptionKeys GetKeys()
    {
        return Keys;
    }
}