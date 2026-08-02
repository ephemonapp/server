using Ephemon.Models.Calls.Infrastructure;

namespace Ephemon.Services.Call.Infrastructure;

public interface ICallProcessor
{
    Task<ICallResponse?> ProcessAsync(ICallRequest request, CancellationToken cancellationToken);
}