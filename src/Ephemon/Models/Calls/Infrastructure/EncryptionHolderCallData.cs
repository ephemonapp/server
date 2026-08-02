namespace Ephemon.Models.Calls.Infrastructure;

internal abstract record EncryptionHolderCallData : TransmittableCallData, IEncryptionHolderCallData
{
    public string PublicEncryptionKey { get; init; } = null!;
}