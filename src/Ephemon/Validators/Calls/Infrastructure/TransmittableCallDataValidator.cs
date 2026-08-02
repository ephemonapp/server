using FluentValidation;
using Ephemon.Extensions;
using Ephemon.Models.Calls.Infrastructure;

namespace Ephemon.Validators.Calls.Infrastructure;

internal abstract class TransmittableCallDataValidator<T> : CallDataValidator<T>
    where T : class, ITransmittableCallData
{
    protected TransmittableCallDataValidator()
    {
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x)
            .Must(x => Math.Abs(x.TimeStamp - x.ServerUnixTimeMilliseconds) <= 5 * 1000)
            .WithMessage(x =>
                $"This message is more than 5 seconds stale (delta: {x.TimeStamp - x.ServerUnixTimeMilliseconds} ms).");

        RuleFor(x => x.PeerPublicKey)
            .NotNull().WithMessage("Public peer key is required.")
            .NotEmpty().WithMessage("Public peer key cannot be empty.")
            .Base64String().WithMessage("Public peer key must be base64 string.");
    }
}