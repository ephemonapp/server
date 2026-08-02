using Ephemon.Models.Calls.Infrastructure;

namespace Ephemon.Services.Call.Infrastructure;

public interface ICallRequestProcessor
{
    Type CallRequestType { get; }
    Task<ICallResponse?> ProcessAsync(ICallRequest request, CancellationToken cancellationToken);
}

public interface ICallRequestProcessor<in TCallRequest> : ICallRequestProcessor
    where TCallRequest : ICallRequest
{
    Task<ICallResponse?> ProcessAsync(TCallRequest request, CancellationToken cancellationToken);
}