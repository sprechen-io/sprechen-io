namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Beendet eine unbenutzte lokale Mikrofonsitzung nach einer Wartezeit.
/// Schlägt das Beenden fehl, wird es nach derselben Wartezeit erneut versucht.
/// </summary>
internal sealed class SpeechCaptureLocalIdleStopFlow : IDisposable
{
    private readonly TimeSpan _idleTimeout;
    private readonly Func<Task<bool>> _canStopAsync;
    private readonly Func<Task> _stopAsync;
    private readonly object _gate = new();

    private CancellationTokenSource? _idleStopCts;
    private bool _disposed;

    internal SpeechCaptureLocalIdleStopFlow(
        TimeSpan idleTimeout,
        Func<Task<bool>> canStopAsync,
        Func<Task> stopAsync)
    {
        _idleTimeout = idleTimeout;
        _canStopAsync = canStopAsync ?? throw new ArgumentNullException(nameof(canStopAsync));
        _stopAsync = stopAsync ?? throw new ArgumentNullException(nameof(stopAsync));
    }

    internal void Start()
    {
        Cancel();

        var idleStopCts = new CancellationTokenSource();
        lock (_gate)
        {
            if (_disposed)
            {
                idleStopCts.Dispose();
                return;
            }

            _idleStopCts = idleStopCts;
        }

        _ = RunIdleStopAsync(idleStopCts);
    }

    internal void Cancel()
    {
        var idleStopCts = ClearCurrent();
        if (idleStopCts is null)
        {
            return;
        }

        try
        {
            idleStopCts.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
        finally
        {
            idleStopCts.Dispose();
        }
    }

    public void Dispose()
    {
        CancellationTokenSource? idleStopCts;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            idleStopCts = _idleStopCts;
            _idleStopCts = null;
        }

        CancelAndDispose(idleStopCts);
    }

    private async Task RunIdleStopAsync(CancellationTokenSource idleStopCts)
    {
        try
        {
            await Task.Delay(_idleTimeout, idleStopCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (!TryClaimCurrent(idleStopCts))
        {
            return;
        }

        try
        {
            if (await _canStopAsync().ConfigureAwait(false))
            {
                await _stopAsync().ConfigureAwait(false);
            }
        }
        catch (Exception)
        {
            // Nach einem fehlgeschlagenen Abbau erneut warten. Eine neue Nutzung
            // kann den Timer abbrechen, bevor ein weiterer Abbau versucht wird.
            Start();
        }
        finally
        {
            idleStopCts.Dispose();
        }
    }

    private CancellationTokenSource? ClearCurrent()
    {
        lock (_gate)
        {
            var idleStopCts = _idleStopCts;
            _idleStopCts = null;
            return idleStopCts;
        }
    }

    private static void CancelAndDispose(CancellationTokenSource? idleStopCts)
    {
        if (idleStopCts is null)
        {
            return;
        }

        try
        {
            idleStopCts.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
        finally
        {
            idleStopCts.Dispose();
        }
    }

    private bool TryClaimCurrent(CancellationTokenSource idleStopCts)
    {
        lock (_gate)
        {
            if (!ReferenceEquals(_idleStopCts, idleStopCts))
            {
                return false;
            }

            _idleStopCts = null;
            return true;
        }
    }
}
