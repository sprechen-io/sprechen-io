using Microsoft.Extensions.Options;

namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Fassade der lokalen Spracherfassung: Sitzungslebenszyklus, Abklingen der Wiedergabe
/// und zugelassene Versuche, damit Aufrufer nicht selbst Slot und Mikrofon koppeln.
/// </summary>
internal sealed class SpeechCaptureClient : ISpeechCaptureClient, IModeAwareSpeechCaptureClient, IAsyncDisposable
{
    private readonly AssistantPlaybackCoordinator _playbackCoordinator;
    private readonly SpeechCaptureSilenceDebounceResolver _silenceDebounceResolver;
    private readonly SpeechCaptureLocalSessionLifecycle _localSessionLifecycle;
    private readonly SpeechCaptureActiveCaptureCoordinator _activeCaptureCoordinator;
    private readonly TimeSpan _shutdownTimeout;
    private readonly object _captureSync = new();
    private readonly HashSet<Task> _captureFences = [];
    private readonly CancellationTokenSource _shutdown = new();
    private Task? _disposeTask;
    private bool _disposed;

    public SpeechCaptureClient(
        AssistantPlaybackCoordinator playbackCoordinator,
        SpeechCaptureSilenceDebounceResolver silenceDebounceResolver,
        SpeechCaptureLocalSessionLifecycle localSessionLifecycle,
        SpeechCaptureActiveCaptureCoordinator activeCaptureCoordinator,
        IOptions<SpeechCaptureClientOptions> captureOptions)
    {
        _playbackCoordinator = playbackCoordinator ?? throw new ArgumentNullException(nameof(playbackCoordinator));
        _silenceDebounceResolver = silenceDebounceResolver ?? throw new ArgumentNullException(nameof(silenceDebounceResolver));
        _localSessionLifecycle = localSessionLifecycle ?? throw new ArgumentNullException(nameof(localSessionLifecycle));
        _activeCaptureCoordinator = activeCaptureCoordinator ?? throw new ArgumentNullException(nameof(activeCaptureCoordinator));
        ArgumentNullException.ThrowIfNull(captureOptions);
        _shutdownTimeout = CaptureShutdownFence.ResolveTimeout(captureOptions.Value.ShutdownTimeout);
    }

    public Task RequestMicrophoneAccessAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        return _localSessionLifecycle.RequestMicrophoneAccessAsync(cancellationToken);
    }

    public async Task EnsureCaptureSessionReadyAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        await RequestMicrophoneAccessAsync(cancellationToken).ConfigureAwait(false);
        await using var ignored = await _localSessionLifecycle.AcquireAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken) => _localSessionLifecycle.StopAsync(cancellationToken);

    public Task PauseAsync(CancellationToken cancellationToken) => _localSessionLifecycle.PauseAsync(cancellationToken);

    public SampleCursor? SnapshotLiveCaptureCursor()
    {
        ThrowIfDisposed();
        return _activeCaptureCoordinator.OpenLiveCaptureCursor();
    }

    public Task<CapturedSpeechAudio> RecordAsync(CancellationToken cancellationToken)
        => RecordAsync(SpeechCaptureMode.Listening, cancellationToken);

    public Task<CapturedSpeechAudio> RecordAsync(SpeechCaptureMode captureMode, CancellationToken cancellationToken)
        => StartOwnedRecord(captureMode, initialSilenceTimeout: null, startCursor: null, cancellationToken);

    public Task<CapturedSpeechAudio> RecordAsync(
        SpeechCaptureMode captureMode,
        TimeSpan initialSilenceTimeout,
        CancellationToken cancellationToken)
        => StartOwnedRecord(captureMode, initialSilenceTimeout, startCursor: null, cancellationToken);

    public Task<CapturedSpeechAudio> RecordAsync(
        SpeechCaptureMode captureMode,
        TimeSpan initialSilenceTimeout,
        SampleCursor startCursor,
        CancellationToken cancellationToken)
        => StartOwnedRecord(captureMode, initialSilenceTimeout, startCursor, cancellationToken);

    public ValueTask DisposeAsync()
    {
        lock (_captureSync)
        {
            return new ValueTask(_disposeTask ??= DisposeCoreAsync());
        }
    }

    private async Task DisposeCoreAsync()
    {
        _shutdown.Cancel();

        Task[] captures;
        lock (_captureSync)
        {
            _disposed = true;
            captures = _captureFences.ToArray();
        }

        try
        {
            await _localSessionLifecycle.ShutdownAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Fehler beim Sitzungsabbau dürfen das Warten auf laufende Aufnahmen nicht überspringen.
        }

        await CaptureShutdownFence.JoinAsync(captures, _shutdownTimeout).ConfigureAwait(false);
    }

    private Task<CapturedSpeechAudio> StartOwnedRecord(
        SpeechCaptureMode captureMode,
        TimeSpan? initialSilenceTimeout,
        SampleCursor? startCursor,
        CancellationToken cancellationToken)
    {
        lock (_captureSync)
        {
            ThrowIfDisposed();
            var capture = RecordAsyncCoreAsync(
                captureMode,
                initialSilenceTimeout,
                startCursor,
                cancellationToken);
            _captureFences.Add(capture);
            _ = RemoveCaptureFenceAsync(capture);
            return capture;
        }
    }

    private async Task RemoveCaptureFenceAsync(Task<CapturedSpeechAudio> capture)
    {
        try
        {
            await capture.ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Der Aufrufer erhält das Ergebnis über die ursprüngliche Task.
        }
        finally
        {
            lock (_captureSync)
            {
                _captureFences.Remove(capture);
            }
        }
    }

    private async Task<CapturedSpeechAudio> RecordAsyncCoreAsync(
        SpeechCaptureMode captureMode,
        TimeSpan? initialSilenceTimeout,
        SampleCursor? startCursor,
        CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        var admission = _activeCaptureCoordinator.AdmitCaptureAttempt();
        using var admittedCapture = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            admission.TerminalCancellation);
        var operationToken = admittedCapture.Token;

        try
        {
            await RequestMicrophoneAccessAsync(operationToken).ConfigureAwait(false);
            await using var sessionLease = await _localSessionLifecycle.AcquireAsync(operationToken).ConfigureAwait(false);

            var behavior = SpeechCaptureModeBehavior.For(captureMode);
            var silenceDebounceMs = await _silenceDebounceResolver
                .GetSilenceDebounceMsAsync(captureMode, operationToken)
                .ConfigureAwait(false);
            var resolution = await _playbackCoordinator.ResolveCaptureStartContextAsync(behavior, operationToken)
                .ConfigureAwait(false);

            // Ein expliziter Cursor hat Vorrang. Andernfalls kann der Koordinator nach
            // Wiedergabe einen früheren Cursor liefern, damit Audio aus der Abklingzeit erhalten bleibt.
            var effectiveStartCursor = startCursor ?? resolution.PlaybackSettleLookbackCursor;

            return await _activeCaptureCoordinator
                .CaptureAdmittedAsync(
                    admission,
                    sessionLease.MimeType,
                    silenceDebounceMs,
                    resolution.Context,
                    behavior,
                    initialSilenceTimeout,
                    effectiveStartCursor,
                    operationToken)
                .ConfigureAwait(false);
        }
        catch (SpeechCaptureStreamStalledException)
        {
            // Ein stehengebliebener Browser-Strom kann lokal noch als erworben gelten.
            // Die Sitzung vor einem erneuten Versuch beenden, damit dieser das Mikrofon neu erwirbt.
            await _localSessionLifecycle.StopAsync(_shutdown.Token).ConfigureAwait(false);
            throw;
        }
        catch (OperationCanceledException) when (admission.TerminalCancellation.IsCancellationRequested)
        {
            _activeCaptureCoordinator.ThrowIfCaptureAdmissionInvalid(admission);
            throw;
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SpeechCaptureClient));
        }
    }
}
