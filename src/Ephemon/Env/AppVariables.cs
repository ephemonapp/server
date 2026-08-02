using Ephemon.Extensions;

namespace Ephemon.Env;

public static class AppVariables
{
    public static string Port => "PORT".GetEnvironmentValueWithFallback("80");

    public static string[] CorsOrigins =>
    [
        .. "CORS".GetEnvironmentValueWithFallback(string.Empty)
            .Trim()
            .Split(',', ';', ' ')
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
    ];

    public static string? PathBase => "PATH_BASE".EnvironmentValue;

    public static string RedisConnectionString =>
        "REDIS_CONNECTION_STRING".RequiredEnvironmentValue;

    public static string MongoConnectionString =>
        "MONGO_CONNECTION_STRING".RequiredEnvironmentValue;
}