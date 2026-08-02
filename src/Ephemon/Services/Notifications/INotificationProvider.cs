namespace Ephemon.Services.Notifications;

public interface INotificationProvider
{
    public Task<PushDeliveryStatus> TrySendAsync(
        ISubscription subscription,
        INotification notification,
        CancellationToken cancellationToken);
}
