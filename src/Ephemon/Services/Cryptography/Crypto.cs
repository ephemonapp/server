using NSec.Cryptography;

namespace Ephemon.Services.Cryptography;

public class Crypto : ICrypto
{
    public bool VerifySignature(byte[] publicKey, byte[] message, byte[] signature)
    {
        return VerifySignatureInternal(publicKey, message, signature);
    }

    private static bool VerifySignatureInternal(byte[] publicKey, byte[] message, byte[] signature)
    {
        try
        {
            var algorithm = SignatureAlgorithm.Ed25519;
            var ed25519PublicKey = PublicKey.Import(algorithm, publicKey, KeyBlobFormat.RawPublicKey);

            return algorithm.Verify(ed25519PublicKey, message, signature);
        }
        catch
        {
            return false;
        }
    }
}