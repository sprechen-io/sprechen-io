namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Vereinheitlicht das explizite Stoppen und asynchrone Freigeben einer Streaming-Sitzung.
/// </summary>
public interface IStreamingSession : IAsyncDisposable
{
    Task StopAsync(CancellationToken cancellationToken);
}


