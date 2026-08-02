using FluentValidation;
using Ephemon.Extensions;
using Ephemon.Models.Calls.Infrastructure;

namespace Ephemon.Validators.Calls.Infrastructure;

internal abstract class CallDataValidator<T> : AbstractValidator<T>
    where T : class, ICallData
{
    protected CallDataValidator()
    {
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.PublicKey)
            .NotNull().WithMessage("Public key is required.")
            .NotEmpty().WithMessage("Public key cannot be empty.")
            .Base64String().WithMessage("Public key must be base64 string.");
    }
}