using FluentValidation;
using Ephemon.Models.Calls.Update;
using Ephemon.Validators.Calls.Infrastructure;

namespace Ephemon.Validators.Calls.Update;

internal sealed class UpdateCallDataValidator : CallDataValidator<UpdateCallData>
{
    public UpdateCallDataValidator()
    {
        ClassLevelCascadeMode = CascadeMode.Stop;

        When(x => x.Subscription != null, () =>
        {
            RuleFor(x => x.Subscription!.Endpoint)
                .NotEmpty().WithMessage("Subscription endpoint is required.");

            RuleFor(x => x.Subscription!.Keys)
                .NotNull().WithMessage("Subscription keys are required.");

            RuleFor(x => x.Subscription!.Keys.P256Dh)
                .NotEmpty().WithMessage("Subscription p256dh key is required.");

            RuleFor(x => x.Subscription!.Keys.Auth)
                .NotEmpty().WithMessage("Subscription auth key is required.");
        });
    }
}