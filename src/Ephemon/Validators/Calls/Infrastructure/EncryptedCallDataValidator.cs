using FluentValidation;
using Ephemon.Extensions;
using Ephemon.Models.Calls.Infrastructure;

namespace Ephemon.Validators.Calls.Infrastructure;

internal abstract class EncryptedCallDataValidator<T> : EncryptionHolderCallDataValidator<T>
    where T : class, IEncryptedCallData
{
    protected EncryptedCallDataValidator()
    {
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.EncryptedDataBase64)
            .NotNull().WithMessage("Encrypted data base64 is required.")
            .NotEmpty().WithMessage("Encrypted data base64 cannot be empty.")
            .Base64String().WithMessage("Encrypted data base64 must be base64 string.");
    }
}