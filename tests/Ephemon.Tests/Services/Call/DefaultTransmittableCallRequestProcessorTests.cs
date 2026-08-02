using Microsoft.AspNetCore.SignalR;
using Moq;
using Ephemon.Data;
using Ephemon.Hubs;
using Ephemon.Models.Calls.Infrastructure;
using Ephemon.Services.Call;
using Ephemon.Services.Notifications;
using Ephemon.Services.Serializers.Infrastructure;
using Ephemon.Storage;

namespace Ephemon.Tests.Services.Call;

[TestClass]
public class DefaultTransmittableCallRequestProcessorTests
{
    private CancellationToken _cancellationToken;
    private Mock<IHubClients> _hubClientsMock = null!;
    private Mock<IHubContext<SignalV1Hub>> _hubContextMock = null!;

    private Mock<IJsonSerializer<ICallData>> _jsonSerializerMock = null!;
    private string _method = null!;
    private Mock<INotificationProvider> _notificationProviderMock = null!;
    private string _peerPublicKey = null!;
    private string _publicKey = null!;
    private string _serializedRequestData = null!;
    private long _serverTimestamp;
    private string _signalRConnectionId = null!;
    private SignalRData _signalRData = null!;
    private Mock<ISignalRDataStorage> _signalRStorageMock = null!;
    private string _signature = null!;
    private Mock<ISingleClientProxy> _singleClientProxyMock = null!;
    private Subscription _subscription = null!;
    private Mock<ISubscriptionStorage> _subscriptionStorageMock = null!;
    private long _timestamp;

    [TestInitialize]
    public void TestInitialize()
    {
        _cancellationToken = CancellationToken.None;
        _publicKey = Unique.String();
        _timestamp = Unique.Int64();
        _serverTimestamp = Unique.Int64();
        _peerPublicKey = Unique.String();
        _method = Unique.String();
        _signature = Unique.String();
        _serializedRequestData = Unique.String();
        _signalRConnectionId = Unique.String();
        _signalRData = new SignalRData(_signalRConnectionId);
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
        _singleClientProxyMock = new Mock<ISingleClientProxy>(MockBehavior.Strict);
        _hubClientsMock = new Mock<IHubClients>(MockBehavior.Strict);
        _hubContextMock = new Mock<IHubContext<SignalV1Hub>>(MockBehavior.Strict);
    }

    [TestMethod]
    public async Task ProcessAsync_Should_Return_Success_CallResponse_If_Transmitted_Via_SignalR()
    {
        // Arrange
        SetupSignalRMocks();
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

    [TestMethod]
    public async Task ProcessAsync_Should_Return_Error_CallResponse_If_Account_Is_Not_Reachable()
    {
        // Arrange
        SetupUnreachableMocks();
        var callRequest = CreateCallRequest();
        var processor = CreateProcessor();

        // Act
        var result = await processor.ProcessAsync(callRequest, _cancellationToken);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Ok);
        Assert.AreEqual(_serverTimestamp, result.Timestamp);
        Assert.AreEqual($"Account {_peerPublicKey} is not reachable.", result.Reason);
        Assert.IsNull(result.Errors);
        VerifyAllMocks();
    }

    [TestMethod]
    public async Task ProcessAsync_Should_Forget_The_Subscription_And_Report_Unreachable_If_Push_Endpoint_Is_Gone()
    {
        // Arrange
        SetupPushOutcomeMocks(PushDeliveryStatus.Expired);
        _subscriptionStorageMock
            .Setup(storage => storage.DeleteAsync(_peerPublicKey, _cancellationToken))
            .ReturnsAsync(true)
            .Verifiable(Times.Once);
        var callRequest = CreateCallRequest();
        var processor = CreateProcessor();

        // Act
        var result = await processor.ProcessAsync(callRequest, _cancellationToken);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Ok);
        Assert.AreEqual($"Account {_peerPublicKey} is not reachable.", result.Reason);
        VerifyAllMocks();
    }

    [TestMethod]
    public async Task ProcessAsync_Should_Keep_The_Subscription_But_Report_Unreachable_If_Push_Failed()
    {
        // Arrange
        SetupPushOutcomeMocks(PushDeliveryStatus.Failed);
        var callRequest = CreateCallRequest();
        var processor = CreateProcessor();

        // Act
        var result = await processor.ProcessAsync(callRequest, _cancellationToken);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Ok);
        Assert.AreEqual($"Account {_peerPublicKey} is not reachable.", result.Reason);
        _subscriptionStorageMock.Verify(
            storage => storage.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        VerifyAllMocks();
    }

    private DummyCallData CreateCallData()
    {
        return new DummyCallData(
            _publicKey,
            _timestamp,
            _peerPublicKey)
        {
            ServerUnixTimeMilliseconds = _serverTimestamp
        };
    }

    private DummyCallRequest CreateCallRequest()
    {
        var request = new DummyCallRequest(
            _method,
            _signature,
            CreateCallData()
        );
        request.SetServerUnixTimeMilliseconds(_serverTimestamp);
        return request;
    }

    private DefaultTransmittableCallRequestProcessor<DummyCallRequest, DummyCallData> CreateProcessor()
    {
        return new DefaultTransmittableCallRequestProcessor<DummyCallRequest, DummyCallData>(
            _jsonSerializerMock.Object,
            _signalRStorageMock.Object,
            _subscriptionStorageMock.Object,
            _notificationProviderMock.Object,
            _hubContextMock.Object);
    }

    private void SetupSignalRMocks()
    {
        _jsonSerializerMock
            .Setup(serializer => serializer.Serialize(It.Is<DummyCallData>(d => d.Equals(CreateCallData()))))
            .Returns(_serializedRequestData)
            .Verifiable(Times.Once);
        _signalRStorageMock
            .Setup(storage => storage.ReadAsync(_peerPublicKey, _cancellationToken))
            .ReturnsAsync(_signalRData)
            .Verifiable(Times.Once);
        _hubClientsMock
            .Setup(clients => clients.Client(_signalRConnectionId))
            .Returns(_singleClientProxyMock.Object)
            .Verifiable(Times.Once);
        _hubContextMock
            .SetupGet(hubContext => hubContext.Clients)
            .Returns(_hubClientsMock.Object)
            .Verifiable(Times.Once);
        _singleClientProxyMock
            .Setup(proxy => proxy.SendCoreAsync(
                "call",
                It.Is<object[]>(args =>
                    args.Length == 1 &&
                    AssertTransmittableData(
                        args[0],
                        _method,
                        _serializedRequestData,
                        _signature)),
                _cancellationToken))
            .Returns(Task.CompletedTask)
            .Verifiable(Times.Once);
    }

    private void SetupPushMocks()
    {
        _jsonSerializerMock
            .Setup(serializer => serializer.Serialize(It.Is<DummyCallData>(d => d.Equals(CreateCallData()))))
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
                It.Is<INotification>(notification => AssertTransmittableNotification(
                    notification,
                    null,
                    null,
                    _method,
                    _serializedRequestData,
                    _signature)),
                _cancellationToken))
            .ReturnsAsync(PushDeliveryStatus.Delivered)
            .Verifiable(Times.Once);
    }

    private void SetupPushOutcomeMocks(PushDeliveryStatus status)
    {
        _jsonSerializerMock
            .Setup(serializer => serializer.Serialize(It.Is<DummyCallData>(d => d.Equals(CreateCallData()))))
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
                It.IsAny<INotification>(),
                _cancellationToken))
            .ReturnsAsync(status)
            .Verifiable(Times.Once);
    }

    private void SetupUnreachableMocks()
    {
        _jsonSerializerMock
            .Setup(serializer => serializer.Serialize(It.Is<DummyCallData>(d => d.Equals(CreateCallData()))))
            .Returns(_serializedRequestData)
            .Verifiable(Times.Once);
        _signalRStorageMock
            .Setup(storage => storage.ReadAsync(_peerPublicKey, _cancellationToken))
            .ReturnsAsync((SignalRData?)null)
            .Verifiable(Times.Once);
        _subscriptionStorageMock
            .Setup(storage => storage.ReadAsync(_peerPublicKey, _cancellationToken))
            .ReturnsAsync((Subscription?)null)
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

        _singleClientProxyMock.VerifyAll();
        _singleClientProxyMock.VerifyNoOtherCalls();

        _hubClientsMock.VerifyAll();
        _hubClientsMock.VerifyNoOtherCalls();

        _hubContextMock.VerifyAll();
        _hubContextMock.VerifyNoOtherCalls();
    }

    private record DummyCallRequest(
        string Method,
        string Signature,
        DummyCallData Data) : ICallRequest<DummyCallData>
    {
        public ICallData GetData()
        {
            return Data;
        }

        public void SetServerUnixTimeMilliseconds(long value)
        {
            Data.ServerUnixTimeMilliseconds = value;
        }
    }

    private record DummyCallData(
        string PublicKey,
        long TimeStamp,
        string PeerPublicKey) : ITransmittableCallData
    {
        public long ServerUnixTimeMilliseconds { get; set; }
    }
}