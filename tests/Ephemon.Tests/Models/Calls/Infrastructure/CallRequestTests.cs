using Ephemon.Models.Calls.Infrastructure;

namespace Ephemon.Tests.Models.Calls.Infrastructure;

[TestClass]
public class CallRequestTests
{
    [TestMethod]
    public void SetServerUnixTimeMilliseconds_Should_Set_Data_ServerUnixTimeMilliseconds_When_Not_Set_Yet()
    {
        var serverUnixTimeMilliseconds = Unique.Int64();
        var request = new DummyCallRequest
        {
            Data = new DummyCallData()
        };

        request.SetServerUnixTimeMilliseconds(serverUnixTimeMilliseconds);

        Assert.AreEqual(serverUnixTimeMilliseconds, request.Data.ServerUnixTimeMilliseconds);
    }

    [TestMethod]
    public void SetServerUnixTimeMilliseconds_Should_Throw_InvalidOperationException_When_Data_ServerUnixTimeMilliseconds_Has_Been_Already_Set()
    {
        var serverUnixTimeMilliseconds = Unique.Int64();
        var request = new DummyCallRequest
        {
            Data = new DummyCallData
            {
                ServerUnixTimeMilliseconds = Unique.Int64()
            }
        };

        Assert.ThrowsExactly<InvalidOperationException>(
            () => request.SetServerUnixTimeMilliseconds(serverUnixTimeMilliseconds),
            "ServerUnixTimeMilliseconds has already been set");
    }

    private record DummyCallRequest : CallRequest<DummyCallData>;

    private record DummyCallData : CallData;
}