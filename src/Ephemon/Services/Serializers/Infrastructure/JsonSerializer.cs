using Newtonsoft.Json;
using Ephemon.Extensions;

namespace Ephemon.Services.Serializers.Infrastructure;

internal class JsonSerializer<T>(JsonSerializerSettings settings) : IJsonSerializer<T>
{
    public virtual string Serialize(T @object)
    {
        if (@object == null) throw new ArgumentNullException(nameof(@object));
        return settings.Serialize(@object);
    }

    public virtual T Deserialize(string json)
    {
        return settings.Deserialize<T>(json);
    }
}