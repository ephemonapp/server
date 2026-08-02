using Newtonsoft.Json;
using Ephemon.Data;
using Ephemon.Extensions;
using Ephemon.Services.Serializers.Infrastructure;

namespace Ephemon.Services.Serializers;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class SignalRDataJsonSerializer(JsonSerializerSettings settings) : JsonSerializer<SignalRData>(settings)
{
    private readonly JsonSerializerSettings _settings = settings;

    public override string Serialize(SignalRData @object)
    {
        return _settings.Serialize(@object.Data);
    }

    public override SignalRData Deserialize(string json)
    {
        return new SignalRData(_settings.Deserialize<string>(json));
    }
}