using Microsoft.Extensions.Logging;
using Sprechen.Speech.State;
using Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant.Exceptions;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Verwaltet Start, Ende und Aufräumen nach Fehlern des Dialoghosts.
/// Diese Aufgaben sind von den fachlichen Zustandsübergängen des Reducers getrennt.
/// </summary>
internal sealed class SpeechAssistantLifecycle : IAsyncDisposable
{
    private const string StateMachineFaultedMessage = "The speech assistant stopped unexpectedly.";

    private readonly SpeechAssistantContext _context;
    private readonly Func<IDialogHost<SpeechAssistantEvent>> _hostFactory;
    private readonly Action<SpeechAssistantState> _stateChanged;
    private readonly Action _resetRuntimeState;
    private readonly SpeechAssistantReducer _reducer = new();
    private readonly SemaphoreSlim _startGate = new(1, 1);

    private IDialogHost<SpeechAssistantEvent>? _host;
    private bool _disposed;

    public SpeechAssistantLifecycle(
        SpeechAssistantContext context,
        Func<IDialogHost<SpeechAssistantEvent>> hostFactory,
        Action<SpeechAssistantState> stateChanged,
        Action resetRuntimeState)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _hostFactory = hostFactory ?? throw new ArgumentNullException(nameof(hostFactory));
        _stateChanged = stateChanged ?? throw new ArgumentNullException(nameof(stateChanged));
        _resetRuntimeState = resetRuntimeState ?? throw new ArgumentNullException(nameof(resetRuntimeState));
    }

    public IDialogHost<SpeechAssistantEvent> GetStartedHostOrThrow(string operationName)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);

        var host = Volatile.Read(ref _host);
        if (host is null)
        {
            throw new SpeechAssistantNotStartedException(operationName);
        }

        return host;
    }

    public async Task<IDialogHost<SpeechAssistantEvent>> EnsureStartedAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        var existing = Volatile.Read(ref _host);
        if (existing is not null)
        {
            return existing;
        }

        await _startGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();

            existing = _host;
            if (existing is not null)
            {
                return existing;
            }

            _resetRuntimeState();
            var flow = DialogFlowFactory.CreateReducerFlow(
                SpeechAssistantState.Initial,
                new SpeechAssistantStateTrackingReducer(_reducer, _stateChanged),
                _context,
                OnStateMachineFaulted);
            var host = _hostFactory();
            await host.StartAsync(flow, cancellationToken).ConfigureAwait(false);

            Volatile.Write(ref _host, host);
            return host;
        }
        finally
        {
            _startGate.Release();
        }
    }

    public async ValueTask StopAsync(CancellationToken cancellationToken)
    {
        IDialogHost<SpeechAssistantEvent>? host;

        await _startGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            host = _host;
            Volatile.Write(ref _host, null);
            _resetRuntimeState();
        }
        finally
        {
            _startGate.Release();
        }

        try
        {
            await _context.InteractionDependencies.SpeechCaptureClient.StopAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _context.InteractionDependencies.Logger.LogDebug(ex, "Stopping speech capture session failed.");
        }

        if (host is not null)
        {
            await host.DisposeAsync().ConfigureAwait(false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            await StopAsync(SpeechAssistantTerminalCancellation.ForRequiredTerminalWork()).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Ein Fehler beim Abbau soll das weitere Freigeben der Lebenszyklussteuerung nicht
            // verhindern.
        }

        _startGate.Dispose();
    }

    public void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SpeechAssistantService));
        }
    }

    private void OnStateMachineFaulted(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (_disposed)
        {
            return;
        }

        _context.InteractionDependencies.Logger.LogError(
            exception,
            "Speech assistant state machine faulted.");

        var host = Volatile.Read(ref _host);
        Volatile.Write(ref _host, null);
        _resetRuntimeState();
        SpeechAssistantTraceFaultObserver.Observe(
            StopCaptureThenPublishFaultedFeedbackAsync(),
            _context.InteractionDependencies.Logger);

        if (host is not null)
        {
            SpeechAssistantTraceFaultObserver.Observe(
                DisposeFaultedHostAsync(host),
                _context.InteractionDependencies.Logger);
        }
    }

    private async ValueTask StopCaptureThenPublishFaultedFeedbackAsync()
    {
        // Versuchen, die Aufnahme vor der Fehlerrückmeldung zu stoppen, damit ein aktives
        // Mikrofon nicht nach dem Fehler weiterläuft. Einen Stoppfehler protokollieren
        // und trotzdem die Fehlerrückmeldung anfordern.
        try
        {
            await _context.InteractionDependencies.SpeechCaptureClient
                .StopAsync(SpeechAssistantTerminalCancellation.ForRequiredTerminalWork())
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _context.InteractionDependencies.Logger.LogDebug(ex, "Stopping speech capture during a state-machine fault failed.");
        }

        await PublishStateMachineFaultedFeedbackAsync().ConfigureAwait(false);
    }

    private async ValueTask PublishStateMachineFaultedFeedbackAsync()
    {
        var signals = _context.InteractionDependencies.SpeechInteractionSignals;
        await signals.SetActivationStateAsync(SpeechAssistantActivationState.Failed).ConfigureAwait(false);
        await signals.ReportFailureAsync(SpeechErrorCode.Unspecified, StateMachineFaultedMessage).ConfigureAwait(false);
    }

    private static async ValueTask DisposeFaultedHostAsync(IDialogHost<SpeechAssistantEvent> host)
    {
        await Task.Yield();
        await host.DisposeAsync().ConfigureAwait(false);
    }
}
