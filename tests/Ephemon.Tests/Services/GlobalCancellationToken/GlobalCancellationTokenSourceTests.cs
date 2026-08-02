using Microsoft.Extensions.Hosting;
using Moq;
using Ephemon.Services.GlobalCancellationToken;

namespace Ephemon.Tests.Services.GlobalCancellationToken;

[TestClass]
public class GlobalCancellationTokenSourceTests
{
    [TestMethod]
    public void Subscribe_CallbackIsRegistered()
    {
        // Arrange
        var (sut, appStoppingCts) = CreateMocks();

        // Act
        appStoppingCts.Cancel();

        // Assert
        Assert.IsTrue(sut.Token.IsCancellationRequested);
    }

    [TestMethod]
    public void PropagateCancellation_InnerTokenIsCancelled()
    {
        // Arrange
        var (globalCancellationTokenSource, applicationStoppingCancellationTokenSource) = CreateMocks();

        // Act & Assert 1
        Assert.IsFalse(globalCancellationTokenSource.Token.IsCancellationRequested);

        // Act 2
        applicationStoppingCancellationTokenSource.Cancel();

        // Assert 2
        Assert.IsTrue(globalCancellationTokenSource.Token.IsCancellationRequested);
    }

    private static (GlobalCancellationTokenSource, CancellationTokenSource) CreateMocks()
    {
        var appStoppingCts = new CancellationTokenSource();

        var lifetimeMock = new Mock<IHostApplicationLifetime>(MockBehavior.Strict);
        lifetimeMock.SetupGet(l => l.ApplicationStopping)
            .Returns(appStoppingCts.Token);

        lifetimeMock.SetupGet(l => l.ApplicationStarted).Returns(CancellationToken.None);
        lifetimeMock.SetupGet(l => l.ApplicationStopped).Returns(CancellationToken.None);

        return (new GlobalCancellationTokenSource(lifetimeMock.Object), appStoppingCts);
    }
}