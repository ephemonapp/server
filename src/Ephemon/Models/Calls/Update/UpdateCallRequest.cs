using Ephemon.Models.Calls.Infrastructure;

namespace Ephemon.Models.Calls.Update;

internal sealed record UpdateCallRequest : CallRequest<UpdateCallData>
{
    public const string MethodName = "update";
}