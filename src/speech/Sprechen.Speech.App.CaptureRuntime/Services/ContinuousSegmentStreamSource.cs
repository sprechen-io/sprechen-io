using Microsoft.Extensions.Options;

namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Hält eine gemeinsame Mikrofonsitzung für aufeinanderfolgende Segmente offen,
/// damit die Erfassung mit dem bisherigen Lesecursor fortgesetzt werden kann.
/// </summary>
internal sealed class ContinuousSegmentStreamSource : IContinuousSegmentStreamSource, IAsyncDisposable
{
    private readonly SpeechCaptureLocalSessionLifecycle _localSessionLifecycle;
    private readonly SpeechCaptureChunkDispatcher _chunkDispatcher;
    private readonly AssistantPlaybackCoordinator _playbackCoordinator;
    private readonly SpeechCaptureSilenceDebounceResolver _silenceDebounceResolver;
    private readonly ActiveSegmentCoordinator _activeSegmentCoordinator;
    private readonly TimeSpan _shutdownTimeout;
    private readonly SemaphoreSlim _listeningGate = new(1, 1);
    private readonly CancellationTokenSource _shutdown = new();
    private readonly object _captureSync = new();
    private readonly HashSet<Task> _captureFences = [];
    private readonly object _disposeSync = new();

    private SpeechCaptureLocalSessionLease? _listeningLease;
    private Task? _disposeTask;
    private SpeechCaptureMode _mode;
    private bool _disposed;

    public ContinuousSegmentStreamSource(
        SpeechCaptureLocalSessionLifecycle localSessionLifecycle,
        SpeechCaptureChunkDispatcher chunkDispatcher,
        AssistantPlaybackCoordinator playbackCoordinator,
        SpeechCaptureSilenceDebounceResolver silenceDebounceResolver,
        ActiveSegmentCoordinator activeSegmentCoordinator,
        IOptions<SpeechCaptureClientOptions> captureOptions)
    {
        _localSessionLifecycle = localSessionLifecycle ?? throw new ArgumentNullException(nameof(localSessionLifecycle));
        _chunkDispatcher = chunkDispatcher ?? throw new ArgumentNullException(nameof(chunkDispatcher));
        _playbackCoordinator = playbackCoordinator ?? throw new ArgumentNullException(nameof(playbackCoordinator));
        _silenceDebounceResolver = silenceDebounceResolver ?? throw new ArgumentNullException(nameof(silenceDebounceResolver));
        _activeSegmentCoordinator = activeSegmentCoordinator ?? throw new ArgumentNullException(nameof(activeSegmentCoordinator));
        ArgumentNullException.ThrowIfNull(captureOptions);
        _shutdownTimeout = CaptureShutdownFence.ResolveTimeout(captureOptions.Value.ShutdownTimeout);
    }

    public async ValueTask BeginListeningAsync(SpeechCaptureMode mode, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _shutdown.Token);

        try
        {
            await BeginListeningCoreAsync(mode, linkedCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
        {
            throw new ObjectDisposedException(nameof(ContinuousSegmentStreamSource));
        }
    }

    private async Task BeginListeningCoreAsync(SpeechCaptureMode mode, CancellationToken cancellationToken)
    {
        await _listeningGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();

            if (_listeningLease is not null)
            {
                return;
            }

            SpeechCaptureLocalSessionLease? lease = null;
            try
            {
                _activeSegmentCoordinator.ResetToLiveEdge();
                lease = await _localSessionLifecycle.AcquireAsync(cancellationToken).ConfigureAwait(false);
                _chunkDispatcher.RegisterAdditionalConsumer(_activeSegmentCoordinator);
                _mode = mode;
                _listeningLease = lease;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (lease is not null)
                {
                    await lease.DisposeAsync().ConfigureAwait(false);
                }

                throw new ContinuousSegmentStreamUnavailableException(
                    "The local continuous segment stream could not start the hot microphone session.",
                    ex);
            }
        }
        finally
        {
            _listeningGate.Release();
        }
    }

    public Task<CapturedSpeechAudio> CaptureNextSegmentAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        var completion = new TaskCompletionSource<CapturedSpeechAudio>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_captureSync)
        {
            ThrowIfDisposed();
            _captureFences.Add(completion.Task);
        }

        _ = CompleteCaptureAsync(completion, cancellationToken);
        return completion.Task;
    }

    private async Task CompleteCaptureAsync(
        TaskCompletionSource<CapturedSpeechAudio> completion,
        CancellationToken cancellationToken)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _shutdown.Token);

        try
        {
            var captured = await CaptureNextSegmentCoreAsync(linkedCts.Token).ConfigureAwait(false);
            completion.TrySetResult(captured);
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
        {
            completion.TrySetException(new ObjectDisposedException(nameof(ContinuousSegmentStreamSource)));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            completion.TrySetCanceled(cancellationToken);
        }
        catch (Exception exception)
        {
            completion.TrySetException(exception);
        }
        finally
        {
            lock (_captureSync)
            {
                _captureFences.Remove(completion.Task);
            }
        }
    }

    private async Task<CapturedSpeechAudio> CaptureNextSegmentCoreAsync(CancellationToken cancellationToken)
    {
        var (listeningLease, attemptId) = await PrepareCaptureAttemptAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var modeBehavior = SpeechCaptureModeBehavior.For(_mode);
            var silenceDebounceMs = await _silenceDebounceResolver
                .GetSilenceDebounceMsAsync(_mode, cancellationToken)
                .ConfigureAwait(false);
            // Der Segmentstrom führt seinen eigenen Lesecursor über mehrere Aufnahmen fort.
            // ResetToLiveEdge setzt ihn beim Sitzungsbeginn, beim Stoppen und bei einer Sitzungserneuerung zurück.
            // Deshalb wird hier nur der Startkontext übernommen. Der zusätzliche Lesecursor
            // nach Wiedergabe ist für eine einzelne Aufnahme ohne fortlaufenden Cursor vorgesehen.
            var resolution = await _playbackCoordinator
                .ResolveCaptureStartContextAsync(modeBehavior, cancellationToken)
                .ConfigureAwait(false);

            try
            {
                return await _activeSegmentCoordinator
                    .CaptureNextSegmentAsync(
                        listeningLease.MimeType,
                        silenceDebounceMs,
                        resolution.Context,
                        modeBehavior,
                        cancellationToken,
                        attemptId)
                    .ConfigureAwait(false);
            }
            catch (SpeechCaptureStreamStalledException)
            {
                // Eine Sitzung ohne weitere Audioblöcke vor der Fehlermeldung beenden,
                // damit der nächste Aufnahmeversuch eine neue Sitzung anfordern kann.
                await _localSessionLifecycle.StopAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }
        finally
        {
            _activeSegmentCoordinator.EndCaptureAttempt(attemptId);
        }
    }

    private async Task<(SpeechCaptureLocalSessionLease Lease, long AttemptId)> PrepareCaptureAttemptAsync(
        CancellationToken cancellationToken)
    {
        await _listeningGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();

            var listeningLease = _listeningLease;
            if (listeningLease is null)
            {
                throw new InvalidOperationException(
                    "ContinuousSegmentStreamSource.CaptureNextSegmentAsync requires BeginListeningAsync to succeed first.");
            }

            if (listeningLease.RecordingCompletion.IsCompleted)
            {
                SpeechCaptureLocalSessionLease replacement;
                try
                {
                    _activeSegmentCoordinator.ResetToLiveEdge();
                    replacement = await _localSessionLifecycle.AcquireAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new ContinuousSegmentStreamUnavailableException(
                        "The local continuous segment stream could not reacquire its ended microphone session.",
                        ex);
                }

                _listeningLease = replacement;
                await listeningLease.DisposeAsync().ConfigureAwait(false);
                listeningLease = replacement;
            }

            var attemptId = _activeSegmentCoordinator.BeginCaptureAttempt();
            if (listeningLease.RecordingCompletion.IsCompleted)
            {
                _activeSegmentCoordinator.FailActiveCapture(new SpeechCaptureSessionEndedException());
            }

            return (listeningLease, attemptId);
        }
        finally
        {
            _listeningGate.Release();
        }
    }

    public async ValueTask StopListeningAsync(CancellationToken cancellationToken)
    {
        if (Volatile.Read(ref _disposed))
        {
            return;
        }

        SpeechCaptureLocalSessionLease? lease;

        await _listeningGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_listeningLease is null)
            {
                return;
            }

            _chunkDispatcher.UnregisterAdditionalConsumer(_activeSegmentCoordinator);
            lease = _listeningLease;
            _listeningLease = null;
            _activeSegmentCoordinator.ResetToLiveEdge();
        }
        finally
        {
            _listeningGate.Release();
        }

        _activeSegmentCoordinator.CancelActiveCapture(CaptureCancellationKind.Abort, cancellationToken);

        if (lease is not null)
        {
            await lease.DisposeAsync().ConfigureAwait(false);
        }
    }

    public ValueTask DisposeAsync()
    {
        lock (_disposeSync)
        {
            return new ValueTask(_disposeTask ??= DisposeCoreAsync());
        }
    }

    private async Task DisposeCoreAsync()
    {
        _shutdown.Cancel();

        SpeechCaptureLocalSessionLease? lease;

        await _listeningGate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            if (_disposed)
            {
                return;
            }

            Volatile.Write(ref _disposed, true);
            lease = _listeningLease;
            if (lease is not null)
            {
                _chunkDispatcher.UnregisterAdditionalConsumer(_activeSegmentCoordinator);
                _listeningLease = null;
                _activeSegmentCoordinator.ResetToLiveEdge();
            }
        }
        finally
        {
            _listeningGate.Release();
        }

        _activeSegmentCoordinator.CancelActiveCapture(CaptureCancellationKind.Abort, CancellationToken.None);
        await JoinCaptureFencesAsync().ConfigureAwait(false);

        if (lease is not null)
        {
            await DisposeListeningLeaseWithinShutdownBoundAsync(lease).ConfigureAwait(false);
        }
    }

    private async Task DisposeListeningLeaseWithinShutdownBoundAsync(SpeechCaptureLocalSessionLease lease)
    {
        var disposal = lease.DisposeAsync().AsTask();
        try
        {
            await disposal.WaitAsync(_shutdownTimeout).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            _ = disposal.ContinueWith(
                static completed => _ = completed.Exception,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
    }

    private async Task JoinCaptureFencesAsync()
    {
        Task[] captures;
        lock (_captureSync)
        {
            captures = _captureFences.ToArray();
        }

        await CaptureShutdownFence.JoinAsync(captures, _shutdownTimeout).ConfigureAwait(false);
    }

    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _disposed) || _shutdown.IsCancellationRequested)
        {
            throw new ObjectDisposedException(nameof(ContinuousSegmentStreamSource));
        }
    }
}
