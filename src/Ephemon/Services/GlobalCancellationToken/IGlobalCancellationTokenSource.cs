namespace Ephemon.Services.GlobalCancellationToken;

public interface IGlobalCancellationTokenSource
{
    CancellationToken Token { get; }
}