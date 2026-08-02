namespace Ephemon.Services.GlobalCancellationToken;

internal sealed class GlobalCancellationTokenSource : IGlobalCancellationTokenSource
{
    private readonly CancellationTokenSource _tokenSource = new();

    public GlobalCancellationTokenSource(IHostApplicationLifetime lifetime)
    {
        lifetime.ApplicationStopping.Register(_tokenSource.Cancel);
    }

    public CancellationToken Token => _tokenSource.Token;
}