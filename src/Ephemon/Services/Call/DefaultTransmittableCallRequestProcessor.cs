using Microsoft.AspNetCore.SignalR;
using Ephemon.Hubs;
using Ephemon.Models.Calls.Infrastructure;
using Ephemon.Services.Call.Infrastructure;
using Ephemon.Services.Notifications;
using Ephemon.Services.Serializers.Infrastructure;
using Ephemon.Storage;

namespace Ephemon.Services.Call;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class DefaultTransmittableCallRequestProcessor<TRequest, TData>(
    IJsonSerializer<ICallData> callDataSerializer,
    ISignalRDataStorage signalRDataStorage,
    ISubscriptionStorage subscriptionStorage,
    INotificationProvider notificationProvider,
    IHubContext<SignalV1Hub> signalHubContext) :
    TransmittableCallRequestProcessor<TRequest, TData>(
        callDataSerializer,
        signalRDataStorage,
        subscriptionStorage,
        notificationProvider,
        signalHubContext)
    where TRequest : ICallRequest<TData>
    where TData : ITransmittableCallData
{
    protected override INotification CreateNotification(Dictionary<string, string> data)
    {
        return new Notification(data);
    }

    private record Notification(Dictionary<string, string> Data) : INotification
    {
        public string? Title => null;
        public string? Body => null;
    }
}