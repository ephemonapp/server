using Ephemon.Models.Calls.Infrastructure;

namespace Ephemon.Models.Calls.Close;

internal sealed record CloseCallRequest : CallRequest<CloseCallData>
{
    public const string MethodName = "close";
}