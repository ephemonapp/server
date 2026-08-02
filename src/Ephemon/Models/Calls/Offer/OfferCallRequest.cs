using Ephemon.Models.Calls.Infrastructure;

namespace Ephemon.Models.Calls.Offer;

internal sealed record OfferCallRequest : CallRequest<OfferCallData>
{
    public const string MethodName = "offer";
}