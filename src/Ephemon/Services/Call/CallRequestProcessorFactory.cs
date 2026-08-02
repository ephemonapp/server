using Ephemon.Models.Calls.Infrastructure;
using Ephemon.Services.Call.Infrastructure;

namespace Ephemon.Services.Call;

internal sealed class CallRequestProcessorFactory(
    IEnumerable<ICallRequestProcessor> callRequestProcessors) : ICallRequestProcessorFactory
{
    public ICallRequestProcessor GetForRequest(ICallRequest request)
    {
        return callRequestProcessors.Single(x => x.CallRequestType == request.GetType());
    }
}