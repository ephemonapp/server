namespace Ephemon.Services.Notifications;

public interface ISubscriptionKeys
{
    public string P256Dh { get; init; }

    public string Auth { get; init; }
}