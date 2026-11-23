using System.Threading.Channels;

namespace Sprechen.Speech.State;

/// <summary>
/// Verarbeitet Ereignisse nacheinander durch den Reducer, übernimmt den neuen
/// Zustand und führt zugehörige Effekte mit einer gemeinsam unterbrechbaren Abbruchquelle aus.
/// </summary>
internal sealed class AsyncReducerStateMachine<TState, TEvent, TContext> : IEventDispatcher<TEvent>, IAsyncDisposable
{
    private readonly object _gate = new();
    private readonly IStateReducer<TState, TEvent, TContext> _reducer;
    private readonly TContext _context;
    private readonly Channel<TEvent> _events;
    private readonly Task _runner;

    private CancellationTokenSource _effectsCts = new();
    private Exception? _fault;
    private bool _disposed;
    private TState _state;

    public AsyncReducerStateMachine(
        TState initialState,
        IStateReducer<TState, TEvent, TContext> reducer,
        TContext context)
    {
        _state = initialState;
        _reducer = reducer ?? throw new ArgumentNullException(nameof(reducer));
        _context = context;

        _events = Channel.CreateUnbounded<TEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });

        _runner = RunAsync();
    }

    public event Action<StateChanged<TState>>? StateChanged;

    /// <summary>
    /// Meldet den ersten Maschinenfehler, etwa aus Reducer, Effekt oder
    /// Zustandsbenachrichtigung, damit der Besitzer den Fehler sichtbar machen und darauf reagieren kann.
    /// </summary>
    public event Action<Exception>? Faulted;

    public TState State
    {
        get
        {
            lock (_gate)
            {
                ThrowIfDisposedOrFaulted();
                return _state;
            }
        }
    }

    public ValueTask DispatchAsync(TEvent @event, DispatchMode mode, CancellationToken cancellationToken)
    {
        if (mode == DispatchMode.Interrupt)
        {
            CancellationTokenSource previous;
            ValueTask queued;

            lock (_gate)
            {
                ThrowIfDisposedOrFaulted();
                previous = _effectsCts;
                _effectsCts = new CancellationTokenSource();
                // Cancellation can synchronously enqueue the interrupted effect's result.
                queued = _events.Writer.WriteAsync(@event, CancellationToken.None);
            }

            try
            {
                previous.Cancel();
            }
            finally
            {
                previous.Dispose();
            }

            return queued;
        }

        lock (_gate)
        {
            ThrowIfDisposedOrFaulted();
        }

        return _events.Writer.WriteAsync(@event, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        CancellationTokenSource effectsCts;

        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            effectsCts = _effectsCts;
        }

        _events.Writer.TryComplete();

        try
        {
            effectsCts.Cancel();
        }
        finally
        {
            effectsCts.Dispose();
        }

        try
        {
            await _runner.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Dieser Abschluss wird beim Abbau ohne weitere Fehlermeldung behandelt.
        }
        catch (Exception)
        {
            // Fehler des Hintergrundlaufs verhindern die Freigabe nicht.
        }
    }

    private async Task RunAsync()
    {
        try
        {
            await foreach (var @event in _events.Reader.ReadAllAsync().ConfigureAwait(false))
            {
                lock (_gate)
                {
                    if (_disposed)
                    {
                        return;
                    }
                }

                var effectsToken = GetEffectsToken();

                StateUpdate<TState, TEvent, TContext> update;
                TState previousState;
                lock (_gate)
                {
                    previousState = _state;
                }

                try
                {
                    update = _reducer.Reduce(previousState, @event);
                }
                catch (Exception ex)
                {
                    Fault(ex);
                    break;
                }

                lock (_gate)
                {
                    _state = update.State;
                }

                try
                {
                    if (!EqualityComparer<TState>.Default.Equals(previousState, update.State))
                    {
                        StateChanged?.Invoke(new StateChanged<TState>(previousState, update.State));
                    }
                }
                catch (Exception ex)
                {
                    Fault(ex);
                    return;
                }

                foreach (var effect in update.Effects)
                {
                    if (effectsToken.IsCancellationRequested)
                    {
                        break;
                    }

                    try
                    {
                        await effect.ExecuteAsync(_context, this, effectsToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (effectsToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Fault(ex);
                        return;
                    }
                }
            }
        }
        catch (ChannelClosedException)
        {
            // Dieser Abschluss wird beim Abbau ohne weitere Fehlermeldung behandelt.
        }
        catch (Exception ex)
        {
            Fault(ex);
        }
    }

    private CancellationToken GetEffectsToken()
    {
        lock (_gate)
        {
            return _effectsCts.Token;
        }
    }

    private void Fault(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        lock (_gate)
        {
            if (_fault is not null)
            {
                return;
            }

            _fault = exception;
        }

        _events.Writer.TryComplete(exception);
        Faulted?.Invoke(exception);
    }

    private void ThrowIfDisposedOrFaulted()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(AsyncReducerStateMachine<TState, TEvent, TContext>));
        }

        if (_fault is not null)
        {
            throw new InvalidOperationException("State machine is faulted.", _fault);
        }
    }
}

