namespace Ephemon.Models.Calls.Infrastructure;

internal abstract record TransmittableCallData : CallData, ITransmittableCallData
{
    public long TimeStamp { get; init; }

    public string PeerPublicKey { get; init; } = null!;
}