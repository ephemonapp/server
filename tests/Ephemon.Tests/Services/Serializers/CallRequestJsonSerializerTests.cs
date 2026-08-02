using System.Reflection;
using Newtonsoft.Json;
using Ephemon.Data;
using Ephemon.Models.Calls.Answer;
using Ephemon.Models.Calls.Close;
using Ephemon.Models.Calls.Dial;
using Ephemon.Models.Calls.Ice;
using Ephemon.Models.Calls.Infrastructure;
using Ephemon.Models.Calls.Offer;
using Ephemon.Models.Calls.Update;
using Ephemon.Services.Serializers;

namespace Ephemon.Tests.Services.Serializers;

[TestClass]
public class CallRequestJsonSerializerTests
{
    public static IEnumerable<object[]> SerializeShouldReturnValidJsonData
    {
        get
        {
            var method = Unique.String();
            var publicKey = Unique.String();
            var endpoint = Unique.Url();
            var expirationTime = Unique.Int64();
            var auth = Unique.String();
            var p256Dh = Unique.String();
            var signature = Unique.String();
            var timestamp = Unique.Int64();
            var peerPublicKey = Unique.String();
            var publicEncryptionKey = Unique.String();
            var encryptedDataBase64 = Unique.String();
            var direction = Unique.Enum<IceDirection>();
            yield return
            [
                new CallRequestBase
                {
                    Method = method,
                    Data = new CallDataBase
                    {
                        PublicKey = publicKey,
                        ServerUnixTimeMilliseconds = Unique.Int64()
                    },
                    Signature = signature
                },
                $"{{\"a\":\"{method}\",\"b\":{{\"a\":\"{publicKey}\"}},\"c\":\"{signature}\"}}"
            ];
            yield return
            [
                new UpdateCallRequest
                {
                    Method = "update",
                    Data = new UpdateCallData
                    {
                        PublicKey = publicKey,
                        Subscription = new Subscription
                        {
                            Endpoint = endpoint,
                            ExpirationTime = expirationTime,
                            Keys = new SubscriptionKeys
                            {
                                P256Dh = p256Dh,
                                Auth = auth
                            }
                        },
                        ServerUnixTimeMilliseconds = Unique.Int64()
                    },
                    Signature = signature
                },
                $"{{\"a\":\"update\",\"b\":{{\"a\":\"{publicKey}\",\"b\":{{\"a\":\"{endpoint}\",\"b\":{expirationTime},\"c\":{{\"a\":\"{p256Dh}\",\"b\":\"{auth}\"}}}}}},\"c\":\"{signature}\"}}"
            ];
            yield return
            [
                new DialCallRequest
                {
                    Method = "dial",
                    Data = new DialCallData
                    {
                        PublicKey = publicKey,
                        TimeStamp = timestamp,
                        PeerPublicKey = peerPublicKey,
                        PublicEncryptionKey = publicEncryptionKey,
                        ServerUnixTimeMilliseconds = Unique.Int64()
                    },
                    Signature = signature
                },
                $"{{\"a\":\"dial\",\"b\":{{\"a\":\"{publicKey}\",\"b\":{timestamp},\"c\":\"{peerPublicKey}\",\"d\":\"{publicEncryptionKey}\"}},\"c\":\"{signature}\"}}"
            ];
            yield return
            [
                new OfferCallRequest
                {
                    Method = "offer",
                    Data = new OfferCallData
                    {
                        PublicKey = publicKey,
                        TimeStamp = timestamp,
                        PeerPublicKey = peerPublicKey,
                        PublicEncryptionKey = publicEncryptionKey,
                        EncryptedDataBase64 = encryptedDataBase64,
                        ServerUnixTimeMilliseconds = Unique.Int64()
                    },
                    Signature = signature
                },
                $"{{\"a\":\"offer\",\"b\":{{\"a\":\"{publicKey}\",\"b\":{timestamp},\"c\":\"{peerPublicKey}\",\"d\":\"{publicEncryptionKey}\",\"e\":\"{encryptedDataBase64}\"}},\"c\":\"{signature}\"}}"
            ];
            yield return
            [
                new AnswerCallRequest
                {
                    Method = "answer",
                    Data = new AnswerCallData
                    {
                        PublicKey = publicKey,
                        TimeStamp = timestamp,
                        PeerPublicKey = peerPublicKey,
                        PublicEncryptionKey = publicEncryptionKey,
                        EncryptedDataBase64 = encryptedDataBase64,
                        ServerUnixTimeMilliseconds = Unique.Int64()
                    },
                    Signature = signature
                },
                $"{{\"a\":\"answer\",\"b\":{{\"a\":\"{publicKey}\",\"b\":{timestamp},\"c\":\"{peerPublicKey}\",\"d\":\"{publicEncryptionKey}\",\"e\":\"{encryptedDataBase64}\"}},\"c\":\"{signature}\"}}"
            ];
            yield return
            [
                new IceCallRequest
                {
                    Method = "ice",
                    Data = new IceCallData
                    {
                        PublicKey = publicKey,
                        TimeStamp = timestamp,
                        PeerPublicKey = peerPublicKey,
                        PublicEncryptionKey = publicEncryptionKey,
                        EncryptedDataBase64 = encryptedDataBase64,
                        Direction = direction,
                        ServerUnixTimeMilliseconds = Unique.Int64()
                    },
                    Signature = signature
                },
                $"{{\"a\":\"ice\",\"b\":{{\"a\":\"{publicKey}\",\"b\":{timestamp},\"c\":\"{peerPublicKey}\",\"d\":\"{publicEncryptionKey}\",\"e\":\"{encryptedDataBase64}\",\"f\":{(int)direction}}},\"c\":\"{signature}\"}}"
            ];
            yield return
            [
                new CloseCallRequest
                {
                    Method = "close",
                    Data = new CloseCallData
                    {
                        PublicKey = publicKey,
                        TimeStamp = timestamp,
                        PeerPublicKey = peerPublicKey,
                        ServerUnixTimeMilliseconds = Unique.Int64()
                    },
                    Signature = signature
                },
                $"{{\"a\":\"close\",\"b\":{{\"a\":\"{publicKey}\",\"b\":{timestamp},\"c\":\"{peerPublicKey}\"}},\"c\":\"{signature}\"}}"
            ];
        }
    }

    public static IEnumerable<object[]> DeserializeShouldReturnValidICallRequestData
    {
        get
        {
            var method = Unique.String();
            var publicKey = Unique.String();
            var endpoint = Unique.Url();
            var expirationTime = Unique.Int64();
            var auth = Unique.String();
            var p256Dh = Unique.String();
            var signature = Unique.String();
            var timestamp = Unique.Int64();
            var peerPublicKey = Unique.String();
            var publicEncryptionKey = Unique.String();
            var encryptedDataBase64 = Unique.String();
            var direction = Unique.Enum<IceDirection>();
            yield return
            [
                $"{{\"a\":\"{method}\",\"b\":{{\"a\":\"{publicKey}\"}},\"c\":\"{signature}\"}}",
                new CallRequestBase
                {
                    Method = method,
                    Data = new CallDataBase
                    {
                        PublicKey = publicKey
                    },
                    Signature = signature
                }
            ];
            yield return
            [
                $"{{\"a\":\"update\",\"b\":{{\"a\":\"{publicKey}\",\"b\":{{\"a\":\"{endpoint}\",\"b\":{expirationTime},\"c\":{{\"a\":\"{p256Dh}\",\"b\":\"{auth}\"}}}}}},\"c\":\"{signature}\"}}",
                new UpdateCallRequest
                {
                    Method = "update",
                    Data = new UpdateCallData
                    {
                        PublicKey = publicKey,
                        Subscription = new Subscription
                        {
                            Endpoint = endpoint,
                            ExpirationTime = expirationTime,
                            Keys = new SubscriptionKeys
                            {
                                P256Dh = p256Dh,
                                Auth = auth
                            }
                        }
                    },
                    Signature = signature
                }
            ];
            yield return
            [
                $"{{\"a\":\"dial\",\"b\":{{\"a\":\"{publicKey}\",\"b\":{timestamp},\"c\":\"{peerPublicKey}\",\"d\":\"{publicEncryptionKey}\"}},\"c\":\"{signature}\"}}",
                new DialCallRequest
                {
                    Method = "dial",
                    Data = new DialCallData
                    {
                        PublicKey = publicKey,
                        TimeStamp = timestamp,
                        PeerPublicKey = peerPublicKey,
                        PublicEncryptionKey = publicEncryptionKey
                    },
                    Signature = signature
                }
            ];
            yield return
            [
                $"{{\"a\":\"offer\",\"b\":{{\"a\":\"{publicKey}\",\"b\":{timestamp},\"c\":\"{peerPublicKey}\",\"d\":\"{publicEncryptionKey}\",\"e\":\"{encryptedDataBase64}\"}},\"c\":\"{signature}\"}}",
                new OfferCallRequest
                {
                    Method = "offer",
                    Data = new OfferCallData
                    {
                        PublicKey = publicKey,
                        TimeStamp = timestamp,
                        PeerPublicKey = peerPublicKey,
                        PublicEncryptionKey = publicEncryptionKey,
                        EncryptedDataBase64 = encryptedDataBase64
                    },
                    Signature = signature
                }
            ];
            yield return
            [
                $"{{\"a\":\"answer\",\"b\":{{\"a\":\"{publicKey}\",\"b\":{timestamp},\"c\":\"{peerPublicKey}\",\"d\":\"{publicEncryptionKey}\",\"e\":\"{encryptedDataBase64}\"}},\"c\":\"{signature}\"}}",
                new AnswerCallRequest
                {
                    Method = "answer",
                    Data = new AnswerCallData
                    {
                        PublicKey = publicKey,
                        TimeStamp = timestamp,
                        PeerPublicKey = peerPublicKey,
                        PublicEncryptionKey = publicEncryptionKey,
                        EncryptedDataBase64 = encryptedDataBase64
                    },
                    Signature = signature
                }
            ];
            yield return
            [
                $"{{\"a\":\"ice\",\"b\":{{\"a\":\"{publicKey}\",\"b\":{timestamp},\"c\":\"{peerPublicKey}\",\"d\":\"{publicEncryptionKey}\",\"e\":\"{encryptedDataBase64}\",\"f\":{(int)direction}}},\"c\":\"{signature}\"}}",
                new IceCallRequest
                {
                    Method = "ice",
                    Data = new IceCallData
                    {
                        PublicKey = publicKey,
                        TimeStamp = timestamp,
                        PeerPublicKey = peerPublicKey,
                        PublicEncryptionKey = publicEncryptionKey,
                        EncryptedDataBase64 = encryptedDataBase64,
                        Direction = direction
                    },
                    Signature = signature
                }
            ];
            yield return
            [
                $"{{\"a\":\"close\",\"b\":{{\"a\":\"{publicKey}\",\"b\":{timestamp},\"c\":\"{peerPublicKey}\"}},\"c\":\"{signature}\"}}",
                new CloseCallRequest
                {
                    Method = "close",
                    Data = new CloseCallData
                    {
                        PublicKey = publicKey,
                        TimeStamp = timestamp,
                        PeerPublicKey = peerPublicKey
                    },
                    Signature = signature
                }
            ];
        }
    }

    [TestMethod]
    [DynamicData(nameof(SerializeShouldReturnValidJsonData), DynamicDataDisplayName = nameof(GetSerializeTestDisplayName))]
    public void Serialize_Should_Return_Valid_Json(
        ICallRequest request,
        string expectedJson)
    {
        // Arrange
        var settings = new JsonSerializerSettings();
        var serializer = new CallRequestJsonSerializer(settings);

        // Act
        var result = serializer.Serialize(request);

        // Assert
        Assert.AreEqual(expectedJson, result);
    }

    [TestMethod]
    [DynamicData(nameof(DeserializeShouldReturnValidICallRequestData), DynamicDataDisplayName = nameof(GetDeserializeTestDisplayName))]
    public void Deserialize_Should_Return_Valid_ICallRequest(
        string json,
        ICallRequest expectedRequest)
    {
        // Arrange
        var settings = new JsonSerializerSettings();
        var serializer = new CallRequestJsonSerializer(settings);

        // Act
        var result = serializer.Deserialize(json);

        // Assert
        Assert.AreEqual(expectedRequest, result);
    }

    public static string GetSerializeTestDisplayName(MethodInfo methodInfo, object[] data)
    {
        return $"{methodInfo.Name} ({data[0].GetType().Name})";
    }

    public static string GetDeserializeTestDisplayName(MethodInfo methodInfo, object[] data)
    {
        return $"{methodInfo.Name} ({data[1].GetType().Name})";
    }
}