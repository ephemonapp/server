using FluentValidation;
using Ephemon.Extensions;
using Ephemon.Models.Calls.Infrastructure;

namespace Ephemon.Validators.Calls.Infrastructure;

internal abstract class EncryptionHolderCallDataValidator<T> : TransmittableCallDataValidator<T>
    where T : class, IEncryptionHolderCallData
{
    protected EncryptionHolderCallDataValidator()
    {
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.PublicEncryptionKey)
            .NotNull().WithMessage("Public encryption key is required.")
            .NotEmpty().WithMessage("Public encryption key cannot be empty.")
            .Base64String().WithMessage("Public encryption key must be base64 string.");
    }
}