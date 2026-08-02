using System.Text;
using FluentValidation;
using Ephemon.Extensions;
using Ephemon.Models.Calls.Answer;
using Ephemon.Models.Calls.Close;
using Ephemon.Models.Calls.Dial;
using Ephemon.Models.Calls.Ice;
using Ephemon.Models.Calls.Infrastructure;
using Ephemon.Models.Calls.Offer;
using Ephemon.Models.Calls.Update;
using Ephemon.Services.Cryptography;
using Ephemon.Services.Serializers.Infrastructure;

namespace Ephemon.Validators.Calls.Infrastructure;

internal sealed class CallRequestValidator : AbstractValidator<ICallRequest>
{
    public CallRequestValidator(
        IValidator<UpdateCallRequest> updateCallRequestValidator,
        IValidator<DialCallRequest> dialCallRequestValidator,
        IValidator<OfferCallRequest> offerCallRequestValidator,
        IValidator<AnswerCallRequest> answerCallRequestValidator,
        IValidator<IceCallRequest> iceCallRequestValidator,
        IValidator<CloseCallRequest> closeCallRequestValidator)
    {
        RuleFor(x => x)
            .SetInheritanceValidator(x =>
            {
                x.Add(updateCallRequestValidator);
                x.Add(dialCallRequestValidator);
                x.Add(offerCallRequestValidator);
                x.Add(answerCallRequestValidator);
                x.Add(iceCallRequestValidator);
                x.Add(closeCallRequestValidator);
            });
    }
}

internal class CallRequestValidator<TRequest, TData> : AbstractValidator<TRequest>
    where TRequest : class, ICallRequest<TData>
    where TData : class, ICallData
{
    public CallRequestValidator(
        IJsonSerializer<ICallData> callDataSerializer,
        IValidator<TData> callDataValidator,
        ICrypto crypto)
    {
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Method)
            .NotEmpty().WithMessage("Method cannot be empty.");

        RuleFor(x => x.Data)
            .SetValidator(callDataValidator);

        RuleFor(x => x.Signature)
            .NotEmpty().WithMessage("Signature cannot be empty.")
            .Base64String().WithMessage("Signature must be base64 string.");

        RuleFor(x => x.Signature)
            .Must((request, _) =>
            {
                var publicKeyBytes = Convert.FromBase64String(request.Data.PublicKey);
                var signatureBytes = Convert.FromBase64String(request.Signature);
                var message = callDataSerializer.Serialize(request.Data);
                var messageBytes = Encoding.UTF8.GetBytes(message);
                return crypto.VerifySignature(publicKeyBytes, messageBytes, signatureBytes);
            }).WithMessage("Invalid signature.");
    }
}