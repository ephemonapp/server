using Ephemon.Models.Calls.Infrastructure;

namespace Ephemon.Services.Call.Infrastructure;

public interface ICallRequestProcessorFactory
{
    ICallRequestProcessor GetForRequest(ICallRequest request);
}