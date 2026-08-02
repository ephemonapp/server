using Ephemon.Models.Calls.Infrastructure;

namespace Ephemon.Services.Call.Infrastructure;

internal abstract class CallRequestProcessor<TCallRequest> : ICallRequestProcessor<TCallRequest>
    where TCallRequest : ICallRequest
{
    public Type CallRequestType => typeof(TCallRequest);

    public Task<ICallResponse?> ProcessAsync(ICallRequest request, CancellationToken cancellationToken)
    {
        if (request is not TCallRequest tRequest)
        {
            throw new ArgumentException($"Request must be of type {nameof(TCallRequest)}", request.GetType().Name);
        }

        return ProcessAsync(tRequest, cancellationToken);
    }

    public abstract Task<ICallResponse?> ProcessAsync(TCallRequest request, CancellationToken cancellationToken);

    protected static ICallResponse CreateSuccessResponse(ICallRequest request)
    {
        return new CallResponse(true, request.GetData().ServerUnixTimeMilliseconds);
    }

    protected static ICallResponse CreateErrorResponse(ICallRequest request, string errorMessage)
    {
        return new CallResponse(false, request.GetData().ServerUnixTimeMilliseconds, errorMessage);
    }
}