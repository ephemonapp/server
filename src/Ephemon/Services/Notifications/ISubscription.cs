namespace Ephemon.Services.Notifications;

public interface ISubscription
{
    public string Endpoint { get; init; }

    public long? ExpirationTime { get; init; }

    public ISubscriptionKeys GetKeys();
}

public interface ISubscription<TSubscriptionKeys> : ISubscription
    where TSubscriptionKeys : ISubscriptionKeys
{
    public TSubscriptionKeys Keys { get; init; }
}