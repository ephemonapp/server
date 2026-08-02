using Moq;
using Ephemon.Models.Calls.Infrastructure;
using Ephemon.Services.Call;
using Ephemon.Services.Call.Infrastructure;

namespace Ephemon.Tests.Services.Call;

[TestClass]
public class CallRequestBaseProcessorTests
{
    [TestMethod]
    public async Task ProcessAsync_Should_Return_Null()
    {
        // Arrange
        var request = new CallRequestBase();
        var cancellationToken = CancellationToken.None;
        var processor = new CallRequestBaseProcessor();

        // Act
        var result = await processor.ProcessAsync(request, cancellationToken);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task ProcessAsync_From_Interface_Should_Call_Abstract_Generic_ProcessAsync_If_Type_Is_Matched()
    {
        // Arrange
        ICallRequest request = new CallRequestBase();
        var cancellationToken = CancellationToken.None;
#pragma warning disable CA1859
        ICallRequestProcessor processor = new CallRequestBaseProcessor();
#pragma warning restore CA1859

        // Act
        var result = await processor.ProcessAsync(request, cancellationToken);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task ProcessAsync_From_Interface_Should_Throw_Exception_ANd_Not_Call_Abstract_Generic_ProcessAsync_If_Type_Is_Not_Matched()
    {
        // Arrange
        var requestMock = new Mock<ICallRequest>(MockBehavior.Strict);
        var processor = new CallRequestBaseProcessor();

        // Act & Assert
        await Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
                await (processor as ICallRequestProcessor)
                    .ProcessAsync(
                        requestMock.Object,
                        CancellationToken.None),
            $"Request must be of type {requestMock.Object.GetType().Name}");
    }
}