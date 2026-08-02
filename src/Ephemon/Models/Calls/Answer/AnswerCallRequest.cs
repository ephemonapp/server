using Ephemon.Models.Calls.Infrastructure;

namespace Ephemon.Models.Calls.Answer;

internal sealed record AnswerCallRequest : CallRequest<AnswerCallData>
{
    public const string MethodName = "answer";
}