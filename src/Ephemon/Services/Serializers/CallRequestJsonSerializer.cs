using Newtonsoft.Json;
using Ephemon.Extensions;
using Ephemon.Models.Calls.Infrastructure;
using Ephemon.Services.Serializers.Infrastructure;

namespace Ephemon.Services.Serializers;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class CallRequestJsonSerializer : JsonSerializer<ICallRequest>
{
    public CallRequestJsonSerializer(JsonSerializerSettings settings) : base(settings)
    {
        settings.AddConverters([JsonConverters.CallRequest]);
    }
}