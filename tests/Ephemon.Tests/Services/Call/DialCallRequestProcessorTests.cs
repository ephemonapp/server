using Microsoft.AspNetCore.SignalR;
using Moq;
using Ephemon.Data;
using Ephemon.Hubs;
using Ephemon.Models.Calls.Dial;
using Ephemon.Models.Calls.Infrastructure;
using Ephemon.Services.Call;
using Ephemon.Services.Notifications;
using Ephemon.Services.Serializers.Infrastructure;
using Ephemon.Storage;

namespace Ephemon.Tests.Services.Call;

[TestClass]
public class DialCallRequestProcessorTests
{
    private CancellationToken _cancellationToken;
    private Mock<IHubContext<SignalV1Hub>> _hubContextMock = null!;

    private Mock<IJsonSerializer<ICallData>> _jsonSerializerMock = null!;
    private Mock<INotificationProvider> _notificationProviderMock = null!;
    private string _peerPublicKey = null!;
    private string _publicEncriptionKey = null!;
    private string _publicKey = null!;
    private string _serializedRequestData = null!;
    private long _serverTimestamp;
    private Mock<ISignalRDataStorage> _signalRStorageMock = null!;
    private string _signature = null!;
    private Subscription _subscription = null!;
    private Mock<ISubscriptionStorage> _subscriptionStorageMock = null!;
    private long _timestamp;

    [TestInitialize]
    public void TestInitialize()
    {
        _cancellationToken = CancellationToken.None;
        _publicKey = Unique.String();
        _timestamp = Unique.Int64();
        _publicEncriptionKey = Unique.String();
        _serverTimestamp = Unique.Int64();
        _peerPublicKey = Unique.String();
        _signature = Unique.String();
        _serializedRequestData = Unique.String();
        _subscription = new Subscription
        {
            Endpoint = Unique.String(),
            ExpirationTime = Unique.Int64(),
            Keys = new SubscriptionKeys
            {
                Auth = Unique.String(),
                P256Dh = Unique.String()
            }
        };
        _jsonSerializerMock = new Mock<IJsonSerializer<ICallData>>(MockBehavior.Strict);
        _signalRStorageMock = new Mock<ISignalRDataStorage>(MockBehavior.Strict);
        _subscriptionStorageMock = new Mock<ISubscriptionStorage>(MockBehavior.Strict);
        _notificationProviderMock = new Mock<INotificationProvider>(MockBehavior.Strict);
        _hubContextMock = new Mock<IHubContext<SignalV1Hub>>(MockBehavior.Strict);
    }

    [TestMethod]
    public async Task ProcessAsync_Should_Return_Success_CallResponse_If_Transmitted_Via_PushNotification()
    {
        // Arrange
        SetupPushMocks();
        var callRequest = CreateCallRequest();
        var processor = CreateProcessor();

        // Act
        var result = await processor.ProcessAsync(callRequest, _cancellationToken);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Ok);
        Assert.AreEqual(_serverTimestamp, result.Timestamp);
        Assert.IsNull(result.Reason);
        Assert.IsNull(result.Errors);
        VerifyAllMocks();
    }

    private DialCallData CreateCallData()
    {
        return new DialCallData
        {
            PublicKey = _publicKey,
            TimeStamp = _timestamp,
            PeerPublicKey = _peerPublicKey,
            PublicEncryptionKey = _publicEncriptionKey,
            ServerUnixTimeMilliseconds = _serverTimestamp
        };
    }

    private DialCallRequest CreateCallRequest()
    {
        return new DialCallRequest
        {
            Method = DialCallRequest.MethodName,
            Signature = _signature,
            Data = CreateCallData()
        };
    }

    private DialCallRequestProcessor CreateProcessor()
    {
        return new DialCallRequestProcessor(
            _jsonSerializerMock.Object,
            _signalRStorageMock.Object,
            _subscriptionStorageMock.Object,
            _notificationProviderMock.Object,
            _hubContextMock.Object);
    }

    private void SetupPushMocks()
    {
        _jsonSerializerMock
            .Setup(serializer => serializer.Serialize(It.Is<DialCallData>(d => d.Equals(CreateCallData()))))
            .Returns(_serializedRequestData)
            .Verifiable(Times.Once);
        _signalRStorageMock
            .Setup(storage => storage.ReadAsync(_peerPublicKey, _cancellationToken))
            .ReturnsAsync((SignalRData?)null)
            .Verifiable(Times.Once);
        _subscriptionStorageMock
            .Setup(storage => storage.ReadAsync(_peerPublicKey, _cancellationToken))
            .ReturnsAsync(_subscription)
            .Verifiable(Times.Once);
        _notificationProviderMock
            .Setup(provider => provider.TrySendAsync(
                _subscription,
                It.Is<INotification>(notification => AssertDialNotification(
                    notification,
                    _serializedRequestData,
                    _signature)),
                _cancellationToken))
            .ReturnsAsync(PushDeliveryStatus.Delivered)
            .Verifiable(Times.Once);
    }

    private static bool AssertTransmittableNotification(
        INotification notification,
        string? title,
        string? body,
        string method,
        string serializedRequestData,
        string signature)
    {
        return notification.Title == title &&
               notification.Body == body &&
               AssertTransmittableData(
                   notification.Data,
                   method,
                   serializedRequestData,
                   signature);
    }

    private static bool AssertTransmittableData(
        object? obj,
        string method,
        string serializedRequestData,
        string signature)
    {
        return obj is Dictionary<string, string>
               {
                   Count: 3
               } data &&
               data["a"] == method &&
               data["b"] == serializedRequestData &&
               data["c"] == signature;
    }

    private static bool AssertDialNotification(
        INotification notification,
        string serializedRequestData,
        string signature)
    {
        return AssertTransmittableNotification(
            notification,
            "🛰️ Hey! Are you here?",
            "Someone is trying to reach you!",
            "dial",
            serializedRequestData,
            signature);
    }

    private void VerifyAllMocks()
    {
        _jsonSerializerMock.VerifyAll();
        _jsonSerializerMock.VerifyNoOtherCalls();
        _signalRStorageMock.VerifyAll();
        _signalRStorageMock.VerifyNoOtherCalls();
        _subscriptionStorageMock.VerifyAll();
        _subscriptionStorageMock.VerifyNoOtherCalls();
        _notificationProviderMock.VerifyAll();
        _notificationProviderMock.VerifyNoOtherCalls();
        _hubContextMock.VerifyAll();
        _hubContextMock.VerifyNoOtherCalls();
    }
}