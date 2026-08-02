using Newtonsoft.Json;

namespace Ephemon.Extensions;

public static class JsonSerializerSettingsExtensions
{
    extension(JsonSerializerSettings jsonSerializerSettings)
    {
        public void AddConverters(IEnumerable<JsonConverter> jsonConverters)
        {
            foreach (var jsonConverter in jsonConverters)
                jsonSerializerSettings.Converters.Add(jsonConverter);
        }

        public string Serialize(object @object)
        {
            return JsonConvert.SerializeObject(@object, jsonSerializerSettings);
        }

        public T Deserialize<T>(string @string)
        {
            return JsonConvert.DeserializeObject<T>(@string, jsonSerializerSettings) ??
                   throw new InvalidOperationException(
                       $"Failed to deserialize string \"{@string}\" as {typeof(T)}.");
        }
    }
}