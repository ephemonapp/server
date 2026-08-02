using FluentValidation;
using Ephemon.Models.Calls.Ice;
using Ephemon.Validators.Calls.Infrastructure;

namespace Ephemon.Validators.Calls.Ice;

internal sealed class IceCallDataValidator : EncryptedCallDataValidator<IceCallData>
{
    public IceCallDataValidator()
    {
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Direction)
            .NotEqual(IceDirection.Unknown).WithMessage("Direction is invalid.");
    }
}