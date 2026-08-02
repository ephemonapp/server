using Moq;
using Ephemon.Models.Calls.Infrastructure;
using Ephemon.Services.Call;
using Ephemon.Services.Call.Infrastructure;

namespace Ephemon.Tests.Services.Call;

[TestClass]
public class CallRequestProcessorFactoryTests
{
    [TestMethod]
    public void GetForRequest_Should_Return_Processor_If_Single_Found_Registered()
    {
        // Arrange
        var request1Mock = new Mock<ICallRequest>(MockBehavior.Strict);
        var request2Mock = new Mock<ICallRequest<ICallData>>(MockBehavior.Strict);
        var processor1Mock = new Mock<ICallRequestProcessor>(MockBehavior.Strict);
        processor1Mock
            .SetupGet(x => x.CallRequestType)
            .Returns(request1Mock.Object.GetType());
        var processor2Mock = new Mock<ICallRequestProcessor>(MockBehavior.Strict);
        processor2Mock
            .SetupGet(x => x.CallRequestType)
            .Returns(request2Mock.Object.GetType());
        var factory = new CallRequestProcessorFactory([
            processor1Mock.Object,
            processor2Mock.Object
        ]);

        // Act
        var processor = factory.GetForRequest(request1Mock.Object);

        // Assert
        Assert.AreEqual(processor1Mock.Object, processor);
    }

    [TestMethod]
    public void GetForRequest_Should_Throw_If_No_Processors_Registered()
    {
        // Arrange
        var requestMock = new Mock<ICallRequest>(MockBehavior.Strict);
        var factory = new CallRequestProcessorFactory([]);

        // Act & Assert
        Assert.ThrowsExactly<InvalidOperationException>(() => factory.GetForRequest(requestMock.Object));
    }

    [TestMethod]
    public void GetForRequest_Should_Throw_If_More_Than_One_Processors_Registered()
    {
        // Arrange
        var requestMock = new Mock<ICallRequest>(MockBehavior.Strict);
        var processor1Mock = new Mock<ICallRequestProcessor>(MockBehavior.Strict);
        processor1Mock
            .SetupGet(x => x.CallRequestType)
            .Returns(requestMock.Object.GetType());
        var processor2Mock = new Mock<ICallRequestProcessor>(MockBehavior.Strict);
        processor2Mock
            .SetupGet(x => x.CallRequestType)
            .Returns(requestMock.Object.GetType());
        var factory = new CallRequestProcessorFactory([
            processor1Mock.Object,
            processor2Mock.Object
        ]);

        // Act & Assert
        Assert.ThrowsExactly<InvalidOperationException>(() => factory.GetForRequest(requestMock.Object));
    }
}