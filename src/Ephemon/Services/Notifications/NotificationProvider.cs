using System.Net;
using Lib.Net.Http.WebPush;
using Ephemon.Services.Serializers.Infrastructure;

namespace Ephemon.Services.Notifications;

internal sealed class NotificationProvider(
    IJsonSerializer<INotification> notificationSerializer,
    IPushServiceClient pushServiceClient) :
    INotificationProvider
{
    public async Task<PushDeliveryStatus> TrySendAsync(
        ISubscription subscription,
        INotification notification,
        CancellationToken cancellationToken)
    {
        var content = notificationSerializer.Serialize(notification);
        var pushSubscription = new PushSubscription
        {
            Endpoint = subscription.Endpoint,
            Keys = new Dictionary<string, string>
            {
                ["p256dh"] = subscription.GetKeys().P256Dh,
                ["auth"] = subscription.GetKeys().Auth
            }
        };
        var pushMessage = new PushMessage(content)
        {
            Urgency = PushMessageUrgency.High
        };

        try
        {
            await pushServiceClient.RequestPushMessageDeliveryAsync(
                pushSubscription,
                pushMessage,
                cancellationToken);
            return PushDeliveryStatus.Delivered;
        }
        catch (PushServiceClientException exception)
        {
            return exception.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone
                ? PushDeliveryStatus.Expired
                : PushDeliveryStatus.Failed;
        }
        catch (HttpRequestException)
        {
            return PushDeliveryStatus.Failed;
        }
    }
}
