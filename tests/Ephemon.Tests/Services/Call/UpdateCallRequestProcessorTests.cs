using Moq;
using Ephemon.Data;
using Ephemon.Models.Calls.Update;
using Ephemon.Services.Call;
using Ephemon.Storage;

namespace Ephemon.Tests.Services.Call;

[TestClass]
public class UpdateCallRequestProcessorTests
{
    private string _auth = null!;
    private CancellationToken _cancellationToken = CancellationToken.None;
    private string _endpoint = null!;
    private long _expirationTime;
    private string _p256Dh = null!;
    private string _publicKey = null!;
    private Subscription _subscription = null!;
    private Mock<ISubscriptionStorage> _subscriptionStorageMock = null!;
    private TimeSpan _timespan;
    private long _timestamp;

    [TestInitialize]
    public void TestInitialize()
    {
        _cancellationToken = CancellationToken.None;
        _publicKey = Unique.String();
        _timestamp = Unique.Int64();
        _endpoint = Unique.String();
        _expirationTime = Unique.Int64();
        _p256Dh = Unique.String();
        _auth = Unique.String();
        _timespan = TimeSpan.FromDays(180);
        _subscription = new Subscription
        {
            Endpoint = _endpoint,
            ExpirationTime = _expirationTime,
            Keys = new SubscriptionKeys
            {
                P256Dh = _p256Dh,
                Auth = _auth
            }
        };
        _subscriptionStorageMock = new Mock<ISubscriptionStorage>(MockBehavior.Strict);
    }

    [TestMethod]
    public async Task ProcessAsync_Should_Upsert_Subscription_If_Presented_And_Return_Success_CallResponse_If_Updated()
    {
        // Arrange
        SetupUpsertAsync(true);
        var request = CreateRequest();
        var processor = CreateProcessor();

        // Act
        var result = await processor
            .ProcessAsync(
                request,
                _cancellationToken);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Ok);
        Assert.AreEqual(_timestamp, result.Timestamp);
        Assert.IsNull(result.Reason);
        Assert.IsNull(result.Errors);
        VerifyAllMocks();
    }

    [TestMethod]
    public async Task ProcessAsync_Should_Upsert_Subscription_If_Presented_And_Return_Error_CallResponse_If_Not_Updated()
    {
        // Arrange
        SetupUpsertAsync(false);
        var request = CreateRequest();
        var processor = CreateProcessor();

        // Act
        var result = await processor
            .ProcessAsync(
                request,
                _cancellationToken);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Ok);
        Assert.AreEqual(_timestamp, result.Timestamp);
        Assert.AreEqual($"Unable to update subscription for {_publicKey}.", result.Reason);
        Assert.IsNull(result.Errors);
        VerifyAllMocks();
    }

    [TestMethod]
    public async Task ProcessAsync_Should_Return_Success_CallResponse_If_Subscription_Is_Not_Presented()
    {
        // Arrange
        var request = CreateRequest(false);
        var processor = CreateProcessor();

        // Act
        var result = await processor
            .ProcessAsync(
                request,
                _cancellationToken);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Ok);
        Assert.AreEqual(_timestamp, result.Timestamp);
        Assert.IsNull(result.Reason);
        Assert.IsNull(result.Errors);
        VerifyAllMocks();
    }

    private UpdateCallRequest CreateRequest(bool withSubscription = true)
    {
        return new UpdateCallRequest
        {
            Method = Unique.String(),
            Data = new UpdateCallData
            {
                PublicKey = _publicKey,
                ServerUnixTimeMilliseconds = _timestamp,
                Subscription = withSubscription ? _subscription : null
            }
        };
    }

    private UpdateCallRequestProcessor CreateProcessor()
    {
        return new UpdateCallRequestProcessor(_subscriptionStorageMock.Object);
    }

    private void SetupUpsertAsync(bool? result)
    {
        _subscriptionStorageMock
            .Setup(storage => storage.UpsertAsync(
                _publicKey,
                _subscription,
                _cancellationToken,
                _timespan))
            .ReturnsAsync(result ?? false)
            .Verifiable(Times.Once);
    }

    private void VerifyAllMocks()
    {
        _subscriptionStorageMock.VerifyAll();
        _subscriptionStorageMock.VerifyNoOtherCalls();
    }
}