using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Ephemon.Data;
using Ephemon.Hubs;
using Ephemon.Models.Calls.Infrastructure;
using Ephemon.Services.Call.Infrastructure;
using Ephemon.Services.GlobalCancellationToken;
using Ephemon.Storage;

namespace Ephemon.Tests.Hubs;

[TestClass]
public class SignalV1HubTests
{
    private Mock<ICallProcessor> _callProcessorMock = null!;
    private CancellationToken _cancellationToken;
    private string _connectionId = null!;
    private Mock<IGlobalCancellationTokenSource> _globalCancellationTokenSourceMock = null!;
    private Mock<ILogger<SignalV1Hub>> _loggerMock = null!;
    private string _publicKey = null!;
    private Mock<ISignalRDataStorage> _signalRDataStorageMock = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _cancellationToken = CancellationToken.None;
        _connectionId = Unique.String();
        _publicKey = Unique.String();
        _signalRDataStorageMock = new Mock<ISignalRDataStorage>(MockBehavior.Strict);
        _callProcessorMock = new Mock<ICallProcessor>(MockBehavior.Strict);
        _loggerMock = new Mock<ILogger<SignalV1Hub>>(MockBehavior.Strict);
        _globalCancellationTokenSourceMock = new Mock<IGlobalCancellationTokenSource>(MockBehavior.Strict);
        _globalCancellationTokenSourceMock
            .SetupGet(source => source.Token)
            .Returns(_cancellationToken)
            .Verifiable(Times.AtLeastOnce);
    }

    [TestMethod]
    public async Task Should_Delete_SignalRData_On_Disconnected()
    {
        // Arrange
        var hubCallerContextMock = new Mock<HubCallerContext>(MockBehavior.Strict);
        hubCallerContextMock
            .SetupGet(context => context.ConnectionId)
            .Returns(_connectionId)
            .Verifiable(Times.Once);
        _signalRDataStorageMock
            .Setup(storage => storage.DeleteAsync(_connectionId, _cancellationToken))
            .ReturnsAsync(true)
            .Verifiable(Times.Once);

        var hub = CreateHub(hubCallerContextMock.Object);

        // Act
        await hub.OnDisconnectedAsync(null);

        // Assert
        VerifyAllMocks();
        hubCallerContextMock.VerifyAll();
        hubCallerContextMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Should_Process_Call_And_Upsert_SignalRData()
    {
        // Arrange
        var hubCallerContextMock = new Mock<HubCallerContext>(MockBehavior.Strict);
        hubCallerContextMock
            .SetupGet(context => context.ConnectionId)
            .Returns(_connectionId)
            .Verifiable(Times.Once);
        var dataMock = new Mock<ICallData>(MockBehavior.Strict);
        dataMock
            .SetupGet(data => data.PublicKey)
            .Returns(_publicKey)
            .Verifiable(Times.Once);

        var requestMock = new Mock<ICallRequest>(MockBehavior.Strict);
        requestMock
            .Setup(request => request.GetData())
            .Returns(dataMock.Object)
            .Verifiable(Times.Once);

        var responseMock = new Mock<ICallResponse>(MockBehavior.Strict);

        _signalRDataStorageMock
            .Setup(storage => storage.UpsertAsync(
                _publicKey,
                It.Is<SignalRData>(data => data.Data == _connectionId),
                _cancellationToken,
                It.Is<TimeSpan>(timespan => timespan == TimeSpan.FromMinutes(2))))
            .ReturnsAsync(true)
            .Verifiable(Times.Once);

        _callProcessorMock
            .Setup(processor => processor.ProcessAsync(requestMock.Object, _cancellationToken))
            .ReturnsAsync(responseMock.Object)
            .Verifiable(Times.Once);

        var hub = CreateHub(hubCallerContextMock.Object);

        // Act
        var result = await hub.CallAsync(requestMock.Object);

        // Assert
        Assert.AreEqual(responseMock.Object, result);
        dataMock.VerifyAll();
        dataMock.VerifyNoOtherCalls();
        requestMock.VerifyAll();
        requestMock.VerifyNoOtherCalls();
        responseMock.VerifyAll();
        responseMock.VerifyNoOtherCalls();
        VerifyAllMocks();
        hubCallerContextMock.VerifyAll();
        hubCallerContextMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Should_Log_And_Throw_Exception_When_Thrown()
    {
        // Arrange
        const string expectedLogMessage = "Internal server error";
        var exception = new Exception(Unique.String());
        var requestMock = new Mock<ICallRequest>(MockBehavior.Strict);
        _callProcessorMock
            .Setup(processor => processor.ProcessAsync(requestMock.Object, _cancellationToken))
            .ThrowsAsync(exception)
            .Verifiable(Times.Once);
        _loggerMock
            .Setup(logger => logger.IsEnabled(LogLevel.Error))
            .Returns(true)
            .Verifiable(Times.Once);
        _loggerMock
            .Setup(logger => logger.Log(
                LogLevel.Error,
#pragma warning disable CA1873
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString() == expectedLogMessage),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
#pragma warning restore CA1873
            .Verifiable(Times.Once);

        var hub = CreateHub();

        // Act & Assert
        await Assert.ThrowsExactlyAsync<ApplicationException>(async () =>
                await hub.CallAsync(requestMock.Object),
            expectedLogMessage);
        requestMock.VerifyAll();
        requestMock.VerifyNoOtherCalls();
        VerifyAllMocks();
    }

    private SignalV1Hub CreateHub(HubCallerContext? context = null)
    {
        var hub = new SignalV1Hub(
            _signalRDataStorageMock.Object,
            _callProcessorMock.Object,
            _loggerMock.Object,
            _globalCancellationTokenSourceMock.Object);
        if (context != null)
            hub.Context = context;
        return hub;
    }

    private void VerifyAllMocks()
    {
        _signalRDataStorageMock.VerifyAll();
        _signalRDataStorageMock.VerifyNoOtherCalls();
        _callProcessorMock.VerifyAll();
        _callProcessorMock.VerifyNoOtherCalls();
        _loggerMock.VerifyAll();
        _loggerMock.VerifyNoOtherCalls();
        _globalCancellationTokenSourceMock.VerifyAll();
        _globalCancellationTokenSourceMock.VerifyNoOtherCalls();
    }
}