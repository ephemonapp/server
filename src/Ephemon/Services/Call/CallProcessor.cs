using FluentValidation;
using Prometheus;
using Ephemon.Models.Calls.Infrastructure;
using Ephemon.Services.Call.Infrastructure;
using Ephemon.Services.DbDateTime;

namespace Ephemon.Services.Call;

internal sealed class CallProcessor(
    IValidator<ICallRequest> callRequestValidator,
    ICallRequestProcessorFactory callRequestProcessorFactory,
    IDbDateTimeProvider dbDateTimeProvider) : ICallProcessor
{
    private static readonly Histogram CallProcessingHistogram = Metrics.CreateHistogram(
        $"{Constants.MetricsPrefix}call_processing",
        "Histogram of call processing.",
        new HistogramConfiguration
        {
            Buckets =
            [
                .001, .002, .005, .01, .02, .05, .1, .2, .5, 1, 2, 5, 10, 30, 60
            ],
            LabelNames =
            [
                "method"
            ]
        });

    public async Task<ICallResponse?> ProcessAsync(ICallRequest request, CancellationToken cancellationToken)
    {
        using (CallProcessingHistogram.WithLabels(request.Method).NewTimer())
        {
            var serverUnixTimeMilliseconds = await dbDateTimeProvider.GetServerTimeAsync();
            request.SetServerUnixTimeMilliseconds(serverUnixTimeMilliseconds);

            var errors = await ValidateAsync(request, cancellationToken);
            if (errors.Count != 0)
                return new CallResponse(
                    false,
                    serverUnixTimeMilliseconds,
                    "There are one or more errors.",
                    errors);

            var callRequestProcessor = callRequestProcessorFactory.GetForRequest(request);
            return await callRequestProcessor.ProcessAsync(request, cancellationToken);
        }
    }

    private async Task<IReadOnlyCollection<string>> ValidateAsync(ICallRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await callRequestValidator.ValidateAsync(request, cancellationToken);
        return !validationResult.IsValid ? validationResult.Errors.Select(e => e.ErrorMessage).ToArray() : [];
    }
}