using Newtonsoft.Json;

namespace Ephemon.Services.Notifications;

public interface INotification
{
    [JsonProperty("title", Order = 0)]
    string? Title { get; }

    [JsonProperty("body", Order = 1)]
    string? Body { get; }

    [JsonProperty("data", Order = 2)]
    Dictionary<string, string> Data { get; }
}