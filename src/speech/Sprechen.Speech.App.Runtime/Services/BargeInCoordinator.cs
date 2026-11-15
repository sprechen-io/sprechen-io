namespace Sprechen.Speech.App.Runtime.Services;

/// <summary>
/// Erneuert beim Benutzereingriff das Abbruchsignal, damit laufende Ausgaben enden und spätere Ausgaben ein frisches Signal erhalten.
/// </summary>
public sealed class BargeInCoordinator : IBargeInCoordinator, IDisposable
{
    private readonly object _gate = new();
    private CancellationTokenSource _cts = new();
    private bool _disposed;

    public CancellationToken Token
    {
        get
        {
            lock (_gate)
            {
                ThrowIfDisposed();
                return _cts.Token;
            }
        }
    }

    public void BargeIn()
    {
        CancellationTokenSource previous;

        lock (_gate)
        {
            ThrowIfDisposed();
            previous = _cts;
            _cts = new CancellationTokenSource();
        }

        try
        {
            previous.Cancel();
        }
        finally
        {
            previous.Dispose();
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        _cts.Dispose();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(BargeInCoordinator));
        }
    }
}


