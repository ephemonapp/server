using Microsoft.AspNetCore.SignalR;
using Ephemon.Hubs;
using Ephemon.Models.Calls.Dial;
using Ephemon.Models.Calls.Infrastructure;
using Ephemon.Services.Call.Infrastructure;
using Ephemon.Services.Notifications;
using Ephemon.Services.Serializers.Infrastructure;
using Ephemon.Storage;

namespace Ephemon.Services.Call;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class DialCallRequestProcessor(
    IJsonSerializer<ICallData> callDataSerializer,
    ISignalRDataStorage signalRDataStorage,
    ISubscriptionStorage subscriptionStorage,
    INotificationProvider notificationProvider,
    IHubContext<SignalV1Hub> signalHubContext) :
    TransmittableCallRequestProcessor<DialCallRequest, DialCallData>(
        callDataSerializer,
        signalRDataStorage,
        subscriptionStorage,
        notificationProvider,
        signalHubContext)
{
    protected override INotification CreateNotification(Dictionary<string, string> data)
    {
        return new DialNotification(data);
    }

    private record DialNotification(Dictionary<string, string> Data) : INotification
    {
        public string Title => "🛰️ Hey! Are you here?";
        public string Body => "Someone is trying to reach you!";
    }
}