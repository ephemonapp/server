using System.Security.Cryptography;

namespace Ephemon.Tests;

public static class Unique
{
    public static long Int64()
    {
        return BitConverter.ToInt64(RandomNumberGenerator.GetBytes(8));
    }

    public static TEnum Enum<TEnum>() where TEnum : struct, Enum
    {
        return RandomNumberGenerator.GetItems(System.Enum.GetValues<TEnum>(), 1)[0];
    }

    public static string String(int? lenght = null, string choices = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_")
    {
        return RandomNumberGenerator.GetString(
            choices, lenght ?? 32);
    }

    public static string Url()
    {
        const string urlSafeChoices = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        return $"https://{String(7, urlSafeChoices)}.{String(7, urlSafeChoices)}.{String(3, urlSafeChoices)}".ToLowerInvariant();
    }
}