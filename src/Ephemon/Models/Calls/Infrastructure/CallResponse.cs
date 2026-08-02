namespace Ephemon.Models.Calls.Infrastructure;

internal sealed record CallResponse(
    bool Ok,
    long Timestamp,
    string? Reason = null,
    IReadOnlyCollection<string>? Errors = null) : ICallResponse;