namespace Ephemon.Models.Calls.Infrastructure;

internal abstract record EncryptedCallData : EncryptionHolderCallData, IEncryptedCallData
{
    public string EncryptedDataBase64 { get; init; } = null!;
}