using Lib.Net.Http.WebPush;

namespace Ephemon.Services.Notifications;

public interface IPushServiceClient
{
    Task RequestPushMessageDeliveryAsync(
        PushSubscription subscription,
        PushMessage message,
        CancellationToken cancellationToken);
}