using Microsoft.Extensions.Options;

namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Koordiniert Erwerb und Freigabe der lokalen Browser-Mikrofonsitzung.
/// Die Sperre verhindert, dass eine neue Aufnahme vor Abschluss der alten Mikrofonfreigabe beginnt.
/// </summary>
internal sealed class SpeechCaptureLocalSessionLifecycle : IAsyncDisposable
{
    private readonly SpeechCaptureSessionCoordinator _sessionCoordinator;
    private readonly SpeechCaptureChunkDispatcher _chunkDispatcher;
    private readonly SpeechCaptureLocalLeaseState _leaseState = new();
    private readonly SpeechCaptureLocalIdleStopFlow _idleStopFlow;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly object _sync = new();
    private readonly CancellationTokenSource _shutdown = new();
    private CancellationTokenSource _admittedOperations = new();
    private readonly TimeSpan _shutdownTimeout;

    private TaskCompletionSource? _stopCompletion;
    private Task? _shutdownTask;
    private bool _disposed;

    public SpeechCaptureLocalSessionLifecycle(
        SpeechCaptureSessionCoordinator sessionCoordinator,
        SpeechCaptureChunkDispatcher chunkDispatcher,
        IOptions<SpeechCaptureClientOptions> captureOptions)
    {
        _sessionCoordinator = sessionCoordinator ?? throw new ArgumentNullException(nameof(sessionCoordinator));
        _chunkDispatcher = chunkDispatcher ?? throw new ArgumentNullException(nameof(chunkDispatcher));
        ArgumentNullException.ThrowIfNull(captureOptions);

        var idleTimeout = captureOptions.Value.LocalSessionIdleTimeout > TimeSpan.Zero
            ? captureOptions.Value.LocalSessionIdleTimeout
            : TimeSpan.FromMinutes(10);
        _shutdownTimeout = CaptureShutdownFence.ResolveTimeout(captureOptions.Value.ShutdownTimeout);
        _idleStopFlow = new SpeechCaptureLocalIdleStopFlow(
            idleTimeout,
            CanStopIdleSessionAsync,
            StopIdleSessionAsync);
    }

    public async Task RequestMicrophoneAccessAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        var stopToken = GetAdmittedOperationToken();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _shutdown.Token,
            stopToken);
        var operationToken = linkedCts.Token;
        await _gate.WaitAsync(operationToken).ConfigureAwait(false);
        try
        {
            await _sessionCoordinator.RequestMicrophoneAccessAsync(operationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<SpeechCaptureLocalSessionLease> AcquireAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        var stopToken = GetAdmittedOperationToken();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _shutdown.Token,
            stopToken);
        var operationToken = linkedCts.Token;
        await _gate.WaitAsync(operationToken).ConfigureAwait(false);
        try
        {
            _idleStopFlow.Cancel();

            await _sessionCoordinator.RequestMicrophoneAccessAsync(operationToken).ConfigureAwait(false);
            var session = await _sessionCoordinator
                .EnsureSessionStartedAsync(
                    _chunkDispatcher.ProcessChunkAsync,
                    _chunkDispatcher.FailActiveConsumers,
                    operationToken)
                .ConfigureAwait(false);

            var leaseGeneration = _leaseState.Acquire();
            return new SpeechCaptureLocalSessionLease(this, session, leaseGeneration);
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        TaskCompletionSource? createdCompletion = null;
        Task completionTask;
        lock (_sync)
        {
            if (_disposed)
            {
                return Task.CompletedTask;
            }

            if (_stopCompletion is null)
            {
                _stopCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                createdCompletion = _stopCompletion;
            }

            completionTask = _stopCompletion.Task;
        }

        if (createdCompletion is not null)
        {
            _ = CompleteStopAsync(createdCompletion);
        }

        return WaitForTerminalOperationAsync(completionTask, cancellationToken);
    }

    private async Task CompleteStopAsync(TaskCompletionSource completion)
    {
        Exception? failure = null;
        try
        {
            await StopCoreAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            failure = exception;
        }

        lock (_sync)
        {
            if (ReferenceEquals(_stopCompletion, completion)
                && (failure is not null || !_shutdown.IsCancellationRequested))
            {
                _stopCompletion = null;
            }

            if (failure is null)
            {
                completion.TrySetResult();
            }
            else
            {
                completion.TrySetException(failure);
            }
        }
    }

    private async Task StopCoreAsync()
    {
        await RunTerminalTransitionAsync(CaptureCancellationKind.Abort).ConfigureAwait(false);
    }

    public async Task PauseAsync(CancellationToken cancellationToken)
    {
        if (_disposed)
        {
            return;
        }

        var pauseTask = PauseCoreAsync();
        await WaitForTerminalOperationAsync(pauseTask, cancellationToken).ConfigureAwait(false);
    }

    private async Task PauseCoreAsync()
    {
        var (canceledOperations, closedOperations) = CloseAdmittedOperations();
        using (canceledOperations)
        {
            await _gate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
            ReopenAdmittedOperations(closedOperations);
            try
            {
                if (_shutdown.IsCancellationRequested)
                {
                    return;
                }

                await RunTerminalTransitionUnderGateAsync(CaptureCancellationKind.Paused).ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
        }
    }

    private async Task RunTerminalTransitionAsync(CaptureCancellationKind cancellationKind)
    {
        var (canceledOperations, closedOperations) = CloseAdmittedOperations();
        using (canceledOperations)
        {
            await _gate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
            ReopenAdmittedOperations(closedOperations);
            try
            {
                await RunTerminalTransitionUnderGateAsync(cancellationKind).ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
        }
    }

    private CancellationToken GetAdmittedOperationToken()
    {
        lock (_sync)
        {
            return _admittedOperations.Token;
        }
    }

    private (CancellationTokenSource Canceled, CancellationTokenSource Closed) CloseAdmittedOperations()
    {
        var closedOperations = new CancellationTokenSource();
        closedOperations.Cancel();
        CancellationTokenSource canceledOperations;
        lock (_sync)
        {
            canceledOperations = _admittedOperations;
            _admittedOperations = closedOperations;
        }

        canceledOperations.Cancel();
        return (canceledOperations, closedOperations);
    }

    private void ReopenAdmittedOperations(CancellationTokenSource closedOperations)
    {
        CancellationTokenSource? replacedOperations = null;
        lock (_sync)
        {
            if (ReferenceEquals(_admittedOperations, closedOperations))
            {
                var reopenedOperations = new CancellationTokenSource();
                if (_shutdown.IsCancellationRequested)
                {
                    reopenedOperations.Cancel();
                }

                _admittedOperations = reopenedOperations;
                replacedOperations = closedOperations;
            }
        }

        replacedOperations?.Dispose();
    }

    private async Task RunTerminalTransitionUnderGateAsync(CaptureCancellationKind cancellationKind)
    {
        _idleStopFlow.Cancel();
        _leaseState.Clear();

        await StopAndReleaseMicrophoneAsync(cancellationKind).ConfigureAwait(false);
    }

    private async Task StopAndReleaseMicrophoneAsync(CaptureCancellationKind cancellationKind)
    {
        var shutdownTask = await _sessionCoordinator
            .StopRecordingAsync(
                ct => _chunkDispatcher.CancelActiveCaptures(cancellationKind, ct),
                CancellationToken.None)
            .ConfigureAwait(false);

        // Die Sperre bis zur Browser-Freigabe halten, damit ein erneuter Erwerb
        // nicht durch die noch ausstehende Freigabe der vorherigen Aufnahme beendet wird.
        await _sessionCoordinator.ReleaseMicrophoneAsync(CancellationToken.None).ConfigureAwait(false);

        // Anwendungscallbacks können nach dem Aufnahmeende noch laufen. Das Mikrofon
        // zuerst freigeben und erst danach auf sie warten. So wird ein Freigabefehler
        // auch bei ausstehenden Callbacks sichtbar und ein weiterer Stop kann es erneut versuchen.
        await _sessionCoordinator
            .WaitForSessionShutdownAsync(shutdownTask, CancellationToken.None)
            .ConfigureAwait(false);
    }

    internal Task ShutdownAsync()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return Task.CompletedTask;
            }

            if (_shutdownTask is null)
            {
                var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var shutdownTask = completion.Task;
                _shutdownTask = shutdownTask;
                _ = CompleteShutdownAsync(completion);
                return shutdownTask;
            }

            return _shutdownTask;
        }
    }

    public ValueTask DisposeAsync() => new(ShutdownAsync());

    private async Task CompleteShutdownAsync(TaskCompletionSource completion)
    {
        try
        {
            await ShutdownCoreAsync().ConfigureAwait(false);
            completion.TrySetResult();
        }
        catch (Exception exception)
        {
            lock (_sync)
            {
                if (ReferenceEquals(_shutdownTask, completion.Task))
                {
                    _shutdownTask = null;
                }
            }

            completion.TrySetException(exception);
        }
    }

    private async Task ShutdownCoreAsync()
    {
        // Den Abbruch vor dem ersten await signalisieren. Dadurch wird auch ein
        // laufender Mikrofonerwerb benachrichtigt, wenn DI den Capture-Client
        // vor dieser Lebenszyklusverwaltung entsorgt.
        _shutdown.Cancel();

        using var timeoutCts = new CancellationTokenSource(_shutdownTimeout);
        try
        {
            await StopAsync(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception) when (timeoutCts.IsCancellationRequested)
        {
            throw new TimeoutException("Microphone lifecycle disposal timed out while cleanup continues.", exception);
        }
        catch (TimeoutException)
        {
            throw;
        }
        catch (Exception stopFailure)
        {
            if (timeoutCts.IsCancellationRequested)
            {
                throw new TimeoutException(
                    "Microphone lifecycle disposal timed out while cleanup continues.",
                    stopFailure);
            }

            try
            {
                await _sessionCoordinator.ReleaseMicrophoneAsync(timeoutCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException releaseFailure) when (timeoutCts.IsCancellationRequested)
            {
                throw new TimeoutException(
                    "Microphone lifecycle disposal timed out while cleanup continues.",
                    new AggregateException(stopFailure, releaseFailure));
            }
            catch (Exception releaseFailure)
            {
                throw new AggregateException(stopFailure, releaseFailure);
            }
        }

        _disposed = true;
        _idleStopFlow.Dispose();
        _admittedOperations.Dispose();
        _shutdown.Dispose();
        // Eine bereits erfolgreiche WaitAsync-Anforderung kann noch auf ihre
        // Fortsetzung warten. Die Sperre nicht entsorgen, damit diese Fortsetzung
        // sie anschließend freigeben kann.
    }

    internal async Task ReleaseAsync(long generation)
    {
        if (_disposed)
        {
            return;
        }

        await _gate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            if (_leaseState.ReleaseAndIsIdle(generation))
            {
                _idleStopFlow.Start();
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed || _shutdown.IsCancellationRequested)
        {
            throw new ObjectDisposedException(nameof(SpeechCaptureLocalSessionLifecycle));
        }
    }

    private static Task WaitForTerminalOperationAsync(Task task, CancellationToken cancellationToken)
        => cancellationToken.CanBeCanceled ? task.WaitAsync(cancellationToken) : task;

    private async Task<bool> CanStopIdleSessionAsync()
    {
        await _gate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            return !_disposed && _leaseState.IsIdle;
        }
        finally
        {
            _gate.Release();
        }
    }

    private Task StopIdleSessionAsync()
    {
        return StopIdleSessionCoreAsync();
    }

    private async Task StopIdleSessionCoreAsync()
    {
        await _gate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            // Die vorherige Idle-Prüfung kann inzwischen veraltet sein. Unter der
            // Sperre erneut prüfen, damit eine neu erworbene Sitzung erhalten bleibt.
            if (_disposed || !_leaseState.IsIdle)
            {
                return;
            }

            _idleStopFlow.Cancel();
            await StopAndReleaseMicrophoneAsync(CaptureCancellationKind.Abort).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }
}
