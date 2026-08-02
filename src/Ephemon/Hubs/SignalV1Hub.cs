using Microsoft.AspNetCore.SignalR;
using Ephemon.Data;
using Ephemon.Models.Calls.Infrastructure;
using Ephemon.Services.Call.Infrastructure;
using Ephemon.Services.GlobalCancellationToken;
using Ephemon.Storage;

namespace Ephemon.Hubs;

public partial class SignalV1Hub(
    ISignalRDataStorage signalRDataStorage,
    ICallProcessor callProcessor,
    ILogger<SignalV1Hub> logger,
    IGlobalCancellationTokenSource cancellationTokenSource) : Hub
{
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await signalRDataStorage
            .DeleteAsync(
                Context.ConnectionId,
                cancellationTokenSource.Token);
        await base.OnDisconnectedAsync(exception);
    }

    [HubMethodName("call")]
    public async Task<ICallResponse?> CallAsync(ICallRequest request)
    {
        ICallResponse? result;
        try
        {
            var cancellationToken = cancellationTokenSource.Token;
            result = await callProcessor.ProcessAsync(request, cancellationToken);
            await UpsertSignalRDataAsync(request.GetData().PublicKey, cancellationToken);
        }
        catch (Exception exception)
        {
            LogInternalServerError(logger, exception);
            throw new ApplicationException("Internal server error");
        }

        return result;
    }

    private Task<bool> UpsertSignalRDataAsync(
        string publicKey,
        CancellationToken cancellationToken)
    {
        return signalRDataStorage.UpsertAsync(
            publicKey,
            new SignalRData(Context.ConnectionId),
            cancellationToken,
            TimeSpan.FromMinutes(2));
    }

    [LoggerMessage(LogLevel.Error, "Internal server error")]
    static partial void LogInternalServerError(ILogger<SignalV1Hub> logger, Exception exception);
}