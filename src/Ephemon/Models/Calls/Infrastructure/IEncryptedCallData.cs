using Newtonsoft.Json;

namespace Ephemon.Models.Calls.Infrastructure;

public interface IEncryptedCallData : IEncryptionHolderCallData
{
    [JsonProperty("e", Required = Required.Always, Order = 4)]
    public string EncryptedDataBase64 { get; init; }
}