using FluentValidation;
using Newtonsoft.Json;
using StackExchange.Redis;
using Ephemon.Data;
using Ephemon.Env;
using Ephemon.Models.Calls.Answer;
using Ephemon.Models.Calls.Close;
using Ephemon.Models.Calls.Dial;
using Ephemon.Models.Calls.Ice;
using Ephemon.Models.Calls.Infrastructure;
using Ephemon.Models.Calls.Offer;
using Ephemon.Models.Calls.Update;
using Ephemon.Services.Call;
using Ephemon.Services.Call.Infrastructure;
using Ephemon.Services.Cryptography;
using Ephemon.Services.Notifications;
using Ephemon.Services.Serializers;
using Ephemon.Services.Serializers.Infrastructure;
using Ephemon.Storage;
using Ephemon.Storage.Redis;
using Ephemon.Validators.Calls.Answer;
using Ephemon.Validators.Calls.Close;
using Ephemon.Validators.Calls.Dial;
using Ephemon.Validators.Calls.Ice;
using Ephemon.Validators.Calls.Infrastructure;
using Ephemon.Validators.Calls.Offer;
using Ephemon.Validators.Calls.Update;

namespace Ephemon.Extensions;

internal static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddCryptography()
        {
            return services
                .AddScoped<ICrypto, Crypto>();
        }

        internal IServiceCollection AddStorage()
        {
            return services
                .AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(AppVariables.RedisConnectionString))
                .AddScoped<ISignalRDataStorage, SignalRDataRedisStorage>()
                .AddScoped<ISubscriptionStorage, SubscriptionRedisStorage>();
        }

        internal IServiceCollection AddCallProcessing()
        {
            return services
                .AddScoped<ICallProcessor, CallProcessor>()
                .AddScoped<ICallRequestProcessorFactory, CallRequestProcessorFactory>()
                .AddCallRequestProcessor<CallRequestBase, CallRequestBaseProcessor>()
                .AddCallRequestProcessor<UpdateCallRequest, UpdateCallRequestProcessor>()
                .AddCallRequestProcessor<DialCallRequest, DialCallRequestProcessor>()
                .AddDefaultTransmittableCallRequestProcessor<OfferCallRequest, OfferCallData>()
                .AddDefaultTransmittableCallRequestProcessor<AnswerCallRequest, AnswerCallData>()
                .AddDefaultTransmittableCallRequestProcessor<IceCallRequest, IceCallData>()
                .AddDefaultTransmittableCallRequestProcessor<CloseCallRequest, CloseCallData>();
        }

        private IServiceCollection AddCallRequestProcessor<TRequest, TProcessor>()
            where TRequest : ICallRequest
            where TProcessor : class, ICallRequestProcessor<TRequest>
        {
            return services
                .AddScoped<ICallRequestProcessor, TProcessor>();
        }

        private IServiceCollection AddDefaultTransmittableCallRequestProcessor<TRequest, TData>()
            where TRequest : ICallRequest<TData>
            where TData : ITransmittableCallData
        {
            return services
                .AddCallRequestProcessor<TRequest, DefaultTransmittableCallRequestProcessor<TRequest, TData>>();
        }

        internal IServiceCollection AddPushServices()
        {
            return services
                .AddMemoryCache()
                .AddMemoryVapidTokenCache()
                .AddPushServiceClient(options =>
                {
                    options.Subject = NotificationVariables.Subject;
                    options.PublicKey = NotificationVariables.PublicKey;
                    options.PrivateKey = NotificationVariables.PrivateKey;
                    options.AutoRetryAfter = true;
                    options.MaxRetriesAfter = -1;
                })
                .AddScoped<IPushServiceClient, DefaultPushServiceClient>()
                .AddScoped<INotificationProvider, NotificationProvider>();
        }

        internal IServiceCollection AddSerializers()
        {
            return services
                .AddSingleton(_ => new JsonSerializerSettings())
                .AddScoped(typeof(IJsonSerializer<>), typeof(JsonSerializer<>))
                .AddJsonSerializer<SignalRData, SignalRDataJsonSerializer>()
                .AddJsonSerializer<ICallRequest, CallRequestJsonSerializer>();
        }

        private IServiceCollection AddJsonSerializer<T, TSerializer>()
            where TSerializer : class, IJsonSerializer<T>
        {
            return services
                .AddScoped<IJsonSerializer<T>, TSerializer>();
        }

        internal IServiceCollection AddValidators()
        {
            return services
                .AddTransient<IValidator<ICallRequest>, CallRequestValidator>()
                .AddTransient<IValidator<UpdateCallRequest>, CallRequestValidator<UpdateCallRequest, UpdateCallData>>()
                .AddTransient<IValidator<UpdateCallData>, UpdateCallDataValidator>()
                .AddTransient<IValidator<DialCallRequest>, CallRequestValidator<DialCallRequest, DialCallData>>()
                .AddTransient<IValidator<DialCallData>, DialCallDataValidator>()
                .AddTransient<IValidator<OfferCallRequest>, CallRequestValidator<OfferCallRequest, OfferCallData>>()
                .AddTransient<IValidator<OfferCallData>, OfferCallDataValidator>()
                .AddTransient<IValidator<AnswerCallRequest>, CallRequestValidator<AnswerCallRequest, AnswerCallData>>()
                .AddTransient<IValidator<AnswerCallData>, AnswerCallDataValidator>()
                .AddTransient<IValidator<IceCallRequest>, CallRequestValidator<IceCallRequest, IceCallData>>()
                .AddTransient<IValidator<IceCallData>, IceCallDataValidator>()
                .AddTransient<IValidator<CloseCallRequest>, CallRequestValidator<CloseCallRequest, CloseCallData>>()
                .AddTransient<IValidator<CloseCallData>, CloseCallDataValidator>();
        }
    }
}