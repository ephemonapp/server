using Ephemon.Models.Calls.Infrastructure;

namespace Ephemon.Models.Calls.Dial;

internal sealed record DialCallRequest : CallRequest<DialCallData>
{
    public const string MethodName = "dial";
}