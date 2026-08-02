using Ephemon.Models.Calls.Infrastructure;
using Ephemon.Models.Calls.Update;
using Ephemon.Services.Call.Infrastructure;
using Ephemon.Storage;

namespace Ephemon.Services.Call;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class UpdateCallRequestProcessor(
    ISubscriptionStorage subscriptionDataStorage) :
    CallRequestProcessor<UpdateCallRequest>
{
    public override async Task<ICallResponse?> ProcessAsync(UpdateCallRequest request, CancellationToken cancellationToken)
    {
        if (request.Data.Subscription is not null)
        {
            return await subscriptionDataStorage.UpsertAsync(
                request.Data.PublicKey,
                request.Data.Subscription,
                cancellationToken,
                TimeSpan.FromDays(180))
                ? CreateSuccessResponse(request)
                : CreateErrorResponse(request, $"Unable to update subscription for {request.Data.PublicKey}.");
        }

        return CreateSuccessResponse(request);
    }
}