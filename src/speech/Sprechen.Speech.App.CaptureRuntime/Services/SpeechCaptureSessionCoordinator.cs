using Microsoft.Extensions.Logging;

namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Verwaltet Start, Ende und Freigabe der Mikrofonsitzung.
/// Die Sitzungsperre bleibt für noch laufende Aufräumoperationen verfügbar.
/// </summary>
internal sealed class SpeechCaptureSessionCoordinator : IDisposable
{
    private readonly IMicrophoneRecorder _microphoneRecorder;
    private readonly SpeechAudioMimeTypeResolver _mimeTypeResolver;
    private readonly SpeechCaptureSessionState _sessionState = new();
    private readonly SpeechCaptureSessionPump _sessionPump;
    private readonly SpeechCaptureRecordingTerminalCleanup _terminalCleanup;
    private readonly SemaphoreSlim _sessionGate = new(1, 1);
    private Task? _pendingSessionShutdown;

    public SpeechCaptureSessionCoordinator(
        IMicrophoneRecorder microphoneRecorder,
        SpeechSampleRing sampleRing,
        SpeechAudioMimeTypeResolver mimeTypeResolver,
        ILogger<SpeechCaptureSessionCoordinator> logger)
    {
        _microphoneRecorder = microphoneRecorder ?? throw new ArgumentNullException(nameof(microphoneRecorder));
        ArgumentNullException.ThrowIfNull(sampleRing);
        _mimeTypeResolver = mimeTypeResolver ?? throw new ArgumentNullException(nameof(mimeTypeResolver));
        ArgumentNullException.ThrowIfNull(logger);
        _sessionPump = new SpeechCaptureSessionPump(sampleRing, logger);
        _terminalCleanup = new SpeechCaptureRecordingTerminalCleanup(logger);
    }

    public async Task RequestMicrophoneAccessAsync(CancellationToken cancellationToken)
    {
        if (_sessionState.MicrophoneAccessRequested)
        {
            return;
        }

        await _sessionGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_sessionState.MicrophoneAccessRequested)
            {
                return;
            }

            await _microphoneRecorder.RequestAccessAsync(cancellationToken).ConfigureAwait(false);
            _sessionState.MarkMicrophoneAccessRequested();
        }
        finally
        {
            _sessionGate.Release();
        }
    }

    public async Task<SpeechCaptureSessionHandle> EnsureSessionStartedAsync(
        Func<byte[], CancellationToken, Task> processChunkAsync,
        Action<Exception> onSessionFailed,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(processChunkAsync);
        ArgumentNullException.ThrowIfNull(onSessionFailed);

        if (_sessionState.TryGetWarmSession(out var warmSession))
        {
            return warmSession;
        }

        await _sessionGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await WaitForPendingSessionShutdownAsync(cancellationToken).ConfigureAwait(false);

            if (_sessionState.TryGetWarmSession(out warmSession))
            {
                return warmSession;
            }

            var completedSession = _sessionState.Clear();
            if (completedSession.HasSession)
            {
                await _terminalCleanup.ResetCompletedSessionAsync(completedSession).ConfigureAwait(false);
                _pendingSessionShutdown = completedSession.SessionTask;
                await WaitForPendingSessionShutdownAsync(cancellationToken).ConfigureAwait(false);
            }

            var sessionCts = new CancellationTokenSource();
            IMicrophoneRecording? recording = null;

            try
            {
                recording = await _microphoneRecorder.StartAsync(cancellationToken).ConfigureAwait(false);
                var mimeType = _mimeTypeResolver.Resolve(recording.MimeType);
                var recordingCompletion = recording.Completion;

                var sessionCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var sessionTask = RunSessionAsync(
                    recording,
                    processChunkAsync,
                    onSessionFailed,
                    sessionCompletion,
                    sessionCts.Token);
                _sessionState.Start(
                    sessionCts,
                    recording,
                    mimeType,
                    sessionTask,
                    sessionCompletion.Task,
                    recordingCompletion);

                return new SpeechCaptureSessionHandle(
                    mimeType,
                    sessionCompletion.Task,
                    recordingCompletion);
            }
            catch
            {
                await _terminalCleanup.DisposeStartFailureAsync(sessionCts, recording).ConfigureAwait(false);
                throw;
            }
        }
        finally
        {
            _sessionGate.Release();
        }
    }

    private async Task WaitForPendingSessionShutdownAsync(CancellationToken cancellationToken)
    {
        await WaitForSessionShutdownAsync(_pendingSessionShutdown, cancellationToken).ConfigureAwait(false);
    }

    public async Task StopAsync(Action<CancellationToken> cancelActiveCapture, CancellationToken cancellationToken)
    {
        var shutdownTask = await StopRecordingAsync(cancelActiveCapture, cancellationToken).ConfigureAwait(false);
        await WaitForSessionShutdownAsync(shutdownTask, cancellationToken).ConfigureAwait(false);
    }

    internal async Task<Task?> StopRecordingAsync(
        Action<CancellationToken> cancelActiveCapture,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cancelActiveCapture);

        SpeechCaptureSessionSnapshot snapshot;

        await _sessionGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            snapshot = _sessionState.Clear();
            if (snapshot.SessionTask is not null)
            {
                _pendingSessionShutdown = snapshot.SessionTask;
            }

            cancelActiveCapture(cancellationToken);
            await _terminalCleanup.StopRecordingAsync(snapshot).ConfigureAwait(false);
            return _pendingSessionShutdown;
        }
        finally
        {
            _sessionGate.Release();
        }
    }

    internal async Task WaitForSessionShutdownAsync(Task? shutdownTask, CancellationToken cancellationToken)
    {
        await _terminalCleanup.WaitForShutdownAsync(shutdownTask, cancellationToken).ConfigureAwait(false);
        if (ReferenceEquals(_pendingSessionShutdown, shutdownTask))
        {
            _pendingSessionShutdown = null;
        }
    }

    public async Task ReleaseMicrophoneAsync(CancellationToken cancellationToken)
    {
        await _sessionGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _sessionState.ResetMicrophoneAccess();
            await _microphoneRecorder.ReleaseAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _sessionGate.Release();
        }
    }

    public void Dispose()
    {
        // Ein zeitlich begrenzter Lebenszyklusstopp kann zurückkehren, während
        // Browser-Aufräumarbeiten noch laufen. Die Sperre nicht entsorgen,
        // damit laufende Operationen sie in ihrem finally freigeben können.
    }

    private async Task RunSessionAsync(
        IMicrophoneRecording recording,
        Func<byte[], CancellationToken, Task> processChunkAsync,
        Action<Exception> onSessionFailed,
        TaskCompletionSource sessionCompletion,
        CancellationToken cancellationToken)
    {
        try
        {
            await _sessionPump
                .RunAsync(
                    recording,
                    processChunkAsync,
                    exception =>
                    {
                        sessionCompletion.TrySetResult();
                        onSessionFailed(exception);
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            sessionCompletion.TrySetResult();
        }
    }

}
