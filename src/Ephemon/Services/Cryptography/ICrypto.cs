namespace Ephemon.Services.Cryptography;

public interface ICrypto
{
    bool VerifySignature(byte[] publicKey, byte[] message, byte[] signature);
}