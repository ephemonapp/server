using Ephemon.Extensions;

namespace Ephemon.Env;

public static class NotificationVariables
{
    public static readonly string Subject = "NOTIFICATION_SUBJECT".RequiredEnvironmentValue;

    public static readonly string
        PublicKey = "NOTIFICATION_PUBLIC_KEY".RequiredEnvironmentValue;

    public static readonly string PrivateKey =
        "NOTIFICATION_PRIVATE_KEY".RequiredEnvironmentValue;
}