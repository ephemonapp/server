using Ephemon.Models.Calls.Infrastructure;

namespace Ephemon.Models.Calls.Ice;

internal sealed record IceCallRequest : CallRequest<IceCallData>
{
    public const string MethodName = "ice";
}