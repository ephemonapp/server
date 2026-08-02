using JsonSubTypes;
using Newtonsoft.Json;
using Ephemon.Models.Calls.Answer;
using Ephemon.Models.Calls.Close;
using Ephemon.Models.Calls.Dial;
using Ephemon.Models.Calls.Ice;
using Ephemon.Models.Calls.Infrastructure;
using Ephemon.Models.Calls.Offer;
using Ephemon.Models.Calls.Update;

namespace Ephemon.Services.Serializers.Infrastructure;

internal static class JsonConverters
{
    private const string CallRequestDiscriminatorPropertyName = "a";

    public static JsonConverter CallRequest =>
        JsonSubtypesConverterBuilder
            .Of<ICallRequest>(CallRequestDiscriminatorPropertyName)
            .SetFallbackSubtype<CallRequestBase>()
            .RegisterSubtype<UpdateCallRequest>(UpdateCallRequest.MethodName)
            .RegisterSubtype<DialCallRequest>(DialCallRequest.MethodName)
            .RegisterSubtype<OfferCallRequest>(OfferCallRequest.MethodName)
            .RegisterSubtype<AnswerCallRequest>(AnswerCallRequest.MethodName)
            .RegisterSubtype<IceCallRequest>(IceCallRequest.MethodName)
            .RegisterSubtype<CloseCallRequest>(CloseCallRequest.MethodName)
            .Build();
}