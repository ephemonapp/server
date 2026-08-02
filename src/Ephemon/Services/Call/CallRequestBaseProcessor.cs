using Ephemon.Models.Calls.Infrastructure;
using Ephemon.Services.Call.Infrastructure;

namespace Ephemon.Services.Call;

internal sealed class CallRequestBaseProcessor : CallRequestProcessor<CallRequestBase>
{
    public override Task<ICallResponse?> ProcessAsync(CallRequestBase request, CancellationToken cancellationToken)
    {
        return Task.FromResult<ICallResponse?>(null);
    }
}