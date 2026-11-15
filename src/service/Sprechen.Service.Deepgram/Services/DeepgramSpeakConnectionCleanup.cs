using Microsoft.Extensions.Logging;

using DeepgramSpeakWebSocketClient = global::Deepgram.Clients.Interfaces.v2.ISpeakWebSocketClient;

namespace Sprechen.Service.Deepgram.Services;

/// <summary>
/// Begrenzt die Wartezeit auf Stop und Close beim Abbau einer Speak-Verbindung.
/// </summary>
internal sealed class DeepgramSpeakConnectionCleanup
{
    private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(2);

    private readonly ILogger _logger;

    public DeepgramSpeakConnectionCleanup(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task CleanupAsync(
        DeepgramSpeakWebSocketClient client,
        CancellationTokenSource clientCts,
        bool connected,
        bool flushed,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(clientCts);

        if (!connected)
        {
            DisposeClient(client);
            return;
        }

        try
        {
            if (flushed)
            {
                // Nach dem Flushed-Signal nur Close senden, ohne zusätzlich auf Stop zu warten.
                await TrySendCloseAsync(client, clientCts, cancellationToken).ConfigureAwait(false);
                return;
            }

            var stopTask = client.Stop(clientCts, nullByte: false);
            await stopTask.WaitAsync(StopTimeout).ConfigureAwait(false);
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Deepgram text-to-speech stop timed out after {Timeout}.", StopTimeout);
            await TrySendCloseAsync(client, clientCts, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Deepgram text-to-speech stop failed.");
            await TrySendCloseAsync(client, clientCts, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            try
            {
                clientCts.Cancel();
            }
            finally
            {
                DisposeClient(client);
            }
        }
    }

    private void DisposeClient(DeepgramSpeakWebSocketClient client)
    {
        try
        {
            ((IDisposable)client).Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Deepgram text-to-speech disposal failed.");
        }
    }

    private async Task TrySendCloseAsync(
        DeepgramSpeakWebSocketClient client,
        CancellationTokenSource clientCts,
        CancellationToken cancellationToken)
    {
        try
        {
            var closeTask = client.SendClose(nullByte: false, _cancellationToken: clientCts);
            await closeTask.WaitAsync(StopTimeout).ConfigureAwait(false);
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Deepgram text-to-speech send close timed out after {Timeout}.", StopTimeout);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug(ex, "Deepgram text-to-speech send close canceled.");
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Deepgram text-to-speech send close failed.");
        }
    }
}
