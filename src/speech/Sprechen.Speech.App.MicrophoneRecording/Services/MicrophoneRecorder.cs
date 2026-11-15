using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Sprechen.Speech.App.MicrophoneRecording.Services;

/// <summary>
/// Besitzt Anforderung, Wiederverwendung und Freigabe der Mikrofonsitzung,
/// damit konkurrierende Aufnahme- und Sperrvorgänge dieselbe Lebenszyklusordnung einhalten.
/// </summary>
internal sealed class MicrophoneRecorder(
    IJSRuntime jsRuntime,
    ILogger<MicrophoneRecorder>? logger = null,
    TimeProvider? timeProvider = null,
    TimeSpan? releaseProgressTimeout = null)
    : IMicrophoneRecorder, ICaptureTrackProcessingReader, IMicGateBrowserBridge, IMicrophoneLifecyclePolicyOwner
{
    private const string SampleRateParameter = "rate=";
    private const int RequiredSampleRateHz = 16_000;

    private const int DefaultChunkMilliseconds = 20;
    private const bool CaptureKeepAliveEnabled = true;
    private static readonly TimeSpan DefaultReleaseProgressTimeout = TimeSpan.FromSeconds(5);

    private readonly object _sync = new();
    private readonly MicrophoneBrowserCapabilities _browser = new(
        jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime)));
    private readonly ILogger<MicrophoneRecorder>? _logger = logger;
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly TimeSpan _releaseProgressTimeout = releaseProgressTimeout is { } configuredTimeout
        && configuredTimeout > TimeSpan.Zero
        ? configuredTimeout
        : DefaultReleaseProgressTimeout;
    private readonly SemaphoreSlim _micGateWriter = new(1, 1);

    private Task<string>? _acquisition;
    private Task<string>? _quarantinedAcquisition;
    private Task? _quarantinedAcquisitionCleanup;
    private string? _sessionId;
    private string? _quarantinedSessionId;
    private MicrophoneRecording? _recording;
    private bool _transitionInProgress;
    private int _lifecycleVersion;
    private long? _lastReleaseTimestamp;
    private TaskCompletionSource? _releaseCompletion;
    private TaskCompletionSource? _stopCompletion;
    private bool _recordingNeedsStopRetry;
    private bool _desiredMicGateEnabled = true;
    private long _desiredMicGateVersion;
    private int _appliedMicGateLifecycleVersion = -1;
    private string? _appliedMicGateSessionId;
    private long _appliedMicGateDesiredVersion = -1;
    private CancellationTokenRegistration _micGateCancellationRegistration;

    /// <summary>
    /// Prüft unter der Sperre, ob der Vorgang noch zur aktuellen Lebenszyklusgeneration gehört.
    /// Nach Freigabe oder Stopp darf ein älterer Vorgang den Zustand nicht mehr ändern.
    /// </summary>
    /// <param name="lifecycleVersion">Die beim Eintritt gelesene Kopie des Generationszählers.</param>
    /// <returns><see langword="true"/>, solange weder Freigabe noch Stopp dazwischengekommen ist.</returns>
    private bool StillOwnsLifecycleLocked(int lifecycleVersion) => lifecycleVersion == _lifecycleVersion;

    /// <summary>
    /// Prüft, ob ein Schreibvorgang für die Mikrofonsperre noch aktuell ist. Dazu müssen
    /// Lebenszyklusgeneration und Sperrversion stimmen sowie die Sitzung unveröffentlicht oder unverändert sein.
    /// </summary>
    /// <param name="lifecycleVersion">Die beim Eintritt gelesene Kopie des Generationszählers.</param>
    /// <param name="desiredVersion">Die Version der Mikrofonsperre, die dieser Schreibvorgang veröffentlichen will.</param>
    /// <param name="unpublishedSessionId">Die noch unveröffentlichte Sitzung, falls es eine gibt.</param>
    /// <param name="sessionId">Die Sitzung, an die geschrieben wurde.</param>
    /// <returns><see langword="true"/>, wenn der Schreibvorgang noch der aktuelle ist.</returns>
    private bool StillOwnsMicGateWriteLocked(
        int lifecycleVersion,
        long desiredVersion,
        string? unpublishedSessionId,
        string sessionId)
        => desiredVersion == _desiredMicGateVersion
            && StillOwnsLifecycleLocked(lifecycleVersion)
            && (unpublishedSessionId is not null || _sessionId == sessionId);

    public async Task<IMicrophoneRecording> StartAsync(CancellationToken cancellationToken)
    {
        return await StartRecordingAsync(DefaultChunkMilliseconds, cancellationToken).ConfigureAwait(false);
    }

    internal async Task<MicrophoneRecording> StartRecordingAsync(
        int timesliceMilliseconds,
        CancellationToken cancellationToken)
    {
        if (timesliceMilliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(timesliceMilliseconds),
                timesliceMilliseconds,
                "Timeslice must be positive.");
        }

        var (recording, acquisition, lifecycleVersion) =
            await ClaimLifecycleAsync().ConfigureAwait(false);

        string sessionId;
        var browserConnectPending = false;
        try
        {
            sessionId = await acquisition.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            lock (_sync)
            {
                if (StillOwnsLifecycleLocked(lifecycleVersion))
                {
                    _recording = null;
                    _transitionInProgress = false;
                }

                if (ReferenceEquals(_acquisition, acquisition)
                    && (acquisition.IsFaulted || acquisition.IsCanceled))
                {
                    _acquisition = null;
                }
            }

            recording.StartFailed(exception);
            throw;
        }

        bool stillOwned;
        lock (_sync)
        {
            stillOwned = StillOwnsLifecycleLocked(lifecycleVersion);

            if (stillOwned)
            {
                _sessionId = sessionId;
                if (ReferenceEquals(_acquisition, acquisition))
                {
                    _acquisition = null;
                }
            }
        }

        if (!stillOwned)
        {
            var stopped = new InvalidOperationException("Microphone recorder was stopped during start.");
            recording.StartFailed(stopped);
            throw stopped;
        }

        try
        {
            await ReconcileMicGateAsync().ConfigureAwait(false);
            lock (_sync)
            {
                stillOwned = StillOwnsLifecycleLocked(lifecycleVersion);
            }

            if (!stillOwned)
            {
                throw new InvalidOperationException("Microphone recorder was stopped during start.");
            }

            browserConnectPending = true;
            var mimeType = await _browser.ConnectAsync(
                    sessionId,
                    recording.CallbackReference,
                    timesliceMilliseconds,
                    cancellationToken)
                .ConfigureAwait(false);
            browserConnectPending = false;
            AssertReportedSampleRate(mimeType);

            lock (_sync)
            {
                stillOwned = StillOwnsLifecycleLocked(lifecycleVersion);
                if (stillOwned)
                {
                    _transitionInProgress = false;
                }
            }

            if (!stillOwned)
            {
                var stopped = new InvalidOperationException("Microphone recorder was stopped during start.");
                throw stopped;
            }

            recording.SetMimeType(mimeType);
            return recording;
        }
        catch (Exception exception)
        {
            var failure = await RecoverFailedStartAsync(
                    lifecycleVersion,
                    recording,
                    sessionId,
                    exception,
                    browserConnectPending,
                    cancellationToken)
                .ConfigureAwait(false);

            recording.StartFailed(failure);

            throw failure;
        }
    }

    /// <summary>
    /// Reserviert den Rekorder für eine neue Aufnahme. Ein zuvor fehlgeschlagener
    /// Stopp wird zunächst wiederholt. Erst nach seinem Erfolg kann die neue Aufnahme beginnen.
    /// </summary>
    /// <returns>Die neue Aufnahme, ihre laufende Mikrofonanforderung und die dabei gelesene Generation.</returns>
    private async Task<(MicrophoneRecording Recording, Task<string> Acquisition, int LifecycleVersion)>
        ClaimLifecycleAsync()
    {
        while (true)
        {
            MicrophoneRecording? retainedFailedStop;
            lock (_sync)
            {
                if (_transitionInProgress)
                {
                    throw new InvalidOperationException("Microphone recorder is already running.");
                }

                retainedFailedStop = _recordingNeedsStopRetry ? _recording : null;
                if (_recording is not null && retainedFailedStop is null)
                {
                    throw new InvalidOperationException("Microphone recorder is already running.");
                }

                if (retainedFailedStop is null)
                {
                    var recording = new MicrophoneRecording(StopRecordingAsync);
                    _recording = recording;
                    _recordingNeedsStopRetry = false;
                    _transitionInProgress = true;
                    var lifecycleVersion = _lifecycleVersion;
                    return (recording, GetOrStartAcquisitionLocked(), lifecycleVersion);
                }
            }

            await StopRecordingAsync(retainedFailedStop, CancellationToken.None).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Bestimmt nach einem gescheiterten Start, welcher Vorgang aufräumt, und fasst
    /// Start- und Aufräumfehler für den Aufrufer zusammen.
    /// </summary>
    /// <param name="lifecycleVersion">Die beim Eintritt gelesene Kopie des Generationszählers.</param>
    /// <param name="recording">Die Aufnahme, deren Start gescheitert ist.</param>
    /// <param name="sessionId">Die erworbene Sitzung, die bei eigener Zuständigkeit freizugeben ist.</param>
    /// <param name="exception">Der Fehler, der den Start abgebrochen hat.</param>
    /// <param name="browserConnectPending">Ob der Abbruch die Browserverbindung traf.</param>
    /// <param name="cancellationToken">Das Abbruchtoken des Aufrufers.</param>
    /// <returns>Der Fehler, mit dem der Start scheitert.</returns>
    private async Task<Exception> RecoverFailedStartAsync(
        int lifecycleVersion,
        MicrophoneRecording recording,
        string sessionId,
        Exception exception,
        bool browserConnectPending,
        CancellationToken cancellationToken)
    {
        Task? supersedingRelease = null;
        Task? supersedingStop = null;
        var ownsFailedStartCleanup = false;
        var retainedForStopRetry = false;
        lock (_sync)
        {
            if (StillOwnsLifecycleLocked(lifecycleVersion))
            {
                ownsFailedStartCleanup = true;
                _recording = null;
                _transitionInProgress = false;
            }
            else
            {
                supersedingRelease = _releaseCompletion?.Task;
                supersedingStop = _stopCompletion?.Task;
                retainedForStopRetry = supersedingRelease is null
                    && supersedingStop is null
                    && ReferenceEquals(_recording, recording)
                    && !_transitionInProgress;
            }
        }

        Exception failure;
        var recoverFailedSupersedingStop = retainedForStopRetry;
        if (ownsFailedStartCleanup)
        {
            failure = await ReleaseAfterFailedConnectionAsync(sessionId, exception).ConfigureAwait(false);
        }
        else
        {
            try
            {
                if (supersedingRelease is not null)
                {
                    await supersedingRelease.ConfigureAwait(false);
                }
                else if (supersedingStop is not null)
                {
                    await supersedingStop.ConfigureAwait(false);
                }

                failure = exception;
            }
            catch (Exception lifecycleFailure)
            {
                recoverFailedSupersedingStop = supersedingStop is not null;
                failure = new AggregateException(exception, lifecycleFailure);
            }
        }

        if (recoverFailedSupersedingStop)
        {
            failure = await RecoverFailedSupersedingStopAsync(recording, failure).ConfigureAwait(false);
        }

        if (browserConnectPending
            && (exception is not OperationCanceledException
                || !cancellationToken.IsCancellationRequested))
        {
            failure = new MicrophoneAcquisitionException(
                MicrophoneAcquisitionFailureCode.Unavailable,
                failure);
        }

        return failure;
    }

    private async Task<Exception> RecoverFailedSupersedingStopAsync(
        MicrophoneRecording recording,
        Exception startFailure)
    {
        try
        {
            await StopRecordingAsync(recording, CancellationToken.None).ConfigureAwait(false);
            return startFailure;
        }
        catch (Exception stopRetryFailure)
        {
            try
            {
                await ReleaseAsync(CancellationToken.None).ConfigureAwait(false);
                return new AggregateException(startFailure, stopRetryFailure);
            }
            catch (Exception releaseFailure)
            {
                return new AggregateException(startFailure, stopRetryFailure, releaseFailure);
            }
        }
    }

    public async Task RequestAccessAsync(CancellationToken cancellationToken)
    {
        Task<string> acquisition;
        int lifecycleVersion;

        lock (_sync)
        {
            if (_transitionInProgress)
            {
                throw new InvalidOperationException("Microphone recorder is changing lifecycle state.");
            }

            lifecycleVersion = _lifecycleVersion;
            acquisition = GetOrStartAcquisitionLocked();
        }

        string sessionId;
        try
        {
            sessionId = await acquisition.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            lock (_sync)
            {
                if (ReferenceEquals(_acquisition, acquisition)
                    && (acquisition.IsFaulted || acquisition.IsCanceled))
                {
                    _acquisition = null;
                }
            }

            throw;
        }

        bool keepSession;
        lock (_sync)
        {
            keepSession = StillOwnsLifecycleLocked(lifecycleVersion);
            if (keepSession)
            {
                _sessionId = sessionId;
                if (ReferenceEquals(_acquisition, acquisition))
                {
                    _acquisition = null;
                }
            }
        }

        if (keepSession)
        {
            await ReconcileMicGateAsync().ConfigureAwait(false);
        }
    }

    public Task ReleaseAsync(CancellationToken cancellationToken)
    {
        TaskCompletionSource completion;
        lock (_sync)
        {
            if (_releaseCompletion is not null)
            {
                return WaitForLifecycleCompletionAsync(_releaseCompletion.Task, cancellationToken);
            }

            completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _releaseCompletion = completion;
        }

        _ = CompleteReleaseAsync(completion);
        return WaitForLifecycleCompletionAsync(completion.Task, cancellationToken);
    }

    private static Task WaitForLifecycleCompletionAsync(Task completion, CancellationToken cancellationToken)
        => cancellationToken.CanBeCanceled ? completion.WaitAsync(cancellationToken) : completion;

    private async Task ReleaseCoreAsync()
    {
        Task<string>? acquisition;
        Task? quarantinedAcquisitionCleanup;
        string? sessionId;
        MicrophoneRecording? recording;
        int lifecycleVersion;

        lock (_sync)
        {
            lifecycleVersion = ++_lifecycleVersion;
            _transitionInProgress = true;
            quarantinedAcquisitionCleanup = _quarantinedAcquisitionCleanup;
            acquisition = _acquisition
                ?? (quarantinedAcquisitionCleanup is null ? _quarantinedAcquisition : null);
            sessionId = _quarantinedSessionId ?? _sessionId;
            recording = _recording;
        }

        var releaseCompleted = false;
        try
        {
            if (acquisition is not null)
            {
                try
                {
                    sessionId = await acquisition
                        .WaitAsync(_releaseProgressTimeout, _timeProvider)
                        .ConfigureAwait(false);
                }
                catch (TimeoutException)
                {
                    QuarantineStalledAcquisition(lifecycleVersion, acquisition);

                    throw;
                }
                catch (Exception)
                {
                    var settlement = ReconcileFailedAcquisition(lifecycleVersion, acquisition);
                    sessionId = settlement.SessionId;

                    if (settlement.RecoveryStillPending)
                    {
                        throw;
                    }
                }
            }
            else if (quarantinedAcquisitionCleanup is not null)
            {
                await quarantinedAcquisitionCleanup
                    .WaitAsync(_releaseProgressTimeout, _timeProvider)
                    .ConfigureAwait(false);

                lock (_sync)
                {
                    if (ReferenceEquals(_quarantinedAcquisitionCleanup, quarantinedAcquisitionCleanup))
                    {
                        _quarantinedAcquisitionCleanup = null;
                    }

                    sessionId = _quarantinedSessionId ?? _sessionId;
                }
            }

            if (sessionId is not null)
            {
                lock (_sync)
                {
                    if (StillOwnsLifecycleLocked(lifecycleVersion))
                    {
                        if (_quarantinedSessionId != sessionId)
                        {
                            _sessionId = sessionId;
                        }

                        if (ReferenceEquals(_acquisition, acquisition))
                        {
                            _acquisition = null;
                        }
                    }
                }

                await ReleaseSessionAsync(sessionId).ConfigureAwait(false);
            }

            releaseCompleted = true;
        }
        finally
        {
            lock (_sync)
            {
                if (ReferenceEquals(_recording, recording))
                {
                    _recording = null;
                    _recordingNeedsStopRetry = false;
                }

                if (StillOwnsLifecycleLocked(lifecycleVersion))
                {
                    if (releaseCompleted)
                    {
                        _acquisition = null;
                        _quarantinedAcquisition = null;
                        _quarantinedAcquisitionCleanup = null;
                        _sessionId = null;
                        _quarantinedSessionId = null;
                        if (sessionId is not null)
                        {
                            _lastReleaseTimestamp = _timeProvider.GetTimestamp();
                        }
                    }
                    else if (sessionId is not null)
                    {
                        _sessionId = sessionId;
                        _quarantinedSessionId = sessionId;
                    }
                }
            }

            recording?.ReleaseCaptureEnded();
        }
    }

    /// <summary>
    /// Behält eine noch laufende Mikrofonanforderung nach Ablauf der Freigabefrist zurück,
    /// damit eine verspätet eintreffende Sitzung weiterhin freigegeben werden kann.
    /// </summary>
    /// <param name="lifecycleVersion">Die beim Eintritt gelesene Kopie des Generationszählers.</param>
    /// <param name="acquisition">Die Mikrofonanforderung, die bis zum Ablauf der Freigabefrist nicht beendet wurde.</param>
    private void QuarantineStalledAcquisition(int lifecycleVersion, Task<string> acquisition)
    {
        lock (_sync)
        {
            if (StillOwnsLifecycleLocked(lifecycleVersion))
            {
                QuarantineAcquisitionLocked(acquisition);
                if (ReferenceEquals(_acquisition, acquisition))
                {
                    _acquisition = null;
                }
            }
        }
    }

    /// <summary>
    /// Räumt nach einem fehlgeschlagenen Mikrofonzugriff auf und meldet, welche Sitzung danach freizugeben
    /// ist und ob eine laufende Wiederherstellung die Zuständigkeit behält.
    /// </summary>
    /// <param name="lifecycleVersion">Die beim Eintritt gelesene Kopie des Generationszählers.</param>
    /// <param name="acquisition">Die fehlgeschlagene Mikrofonanforderung.</param>
    /// <returns>Die freizugebende Sitzung und ob die Wiederherstellung weiterläuft.</returns>
    private (string? SessionId, bool RecoveryStillPending) ReconcileFailedAcquisition(
        int lifecycleVersion,
        Task<string> acquisition)
    {
        lock (_sync)
        {
            var recoveryStillPending = _quarantinedAcquisition is not null
                && !ReferenceEquals(_quarantinedAcquisition, acquisition);
            if (recoveryStillPending)
            {
                // Die laufende Wiederherstellung bleibt allein für die ursprüngliche Anforderung zuständig.
                // Eine verspätete Sitzung bleibt trotz typisiertem Fehler möglich.
                _acquisition = null;
            }

            if (!recoveryStillPending && StillOwnsLifecycleLocked(lifecycleVersion))
            {
                _acquisition = null;
            }

            if (!recoveryStillPending
                && ReferenceEquals(_quarantinedAcquisition, acquisition))
            {
                _quarantinedAcquisition = null;
            }

            return (_quarantinedSessionId ?? _sessionId, recoveryStillPending);
        }
    }

    private async Task CompleteReleaseAsync(TaskCompletionSource completion)
    {
        try
        {
            await ReleaseCoreAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            lock (_sync)
            {
                if (ReferenceEquals(_releaseCompletion, completion))
                {
                    completion.TrySetException(exception);
                    _releaseCompletion = null;
                    _transitionInProgress = false;
                }
            }

            return;
        }

        lock (_sync)
        {
            if (ReferenceEquals(_releaseCompletion, completion))
            {
                completion.TrySetResult();
                _releaseCompletion = null;
                _transitionInProgress = false;
            }
        }
    }

    public async Task SetMicGateAsync(bool enabled, CancellationToken cancellationToken)
    {
        long desiredVersion;
        CancellationTokenRegistration previousRegistration;
        lock (_sync)
        {
            previousRegistration = _micGateCancellationRegistration;
            _micGateCancellationRegistration = default;
            _desiredMicGateEnabled = enabled;
            desiredVersion = ++_desiredMicGateVersion;
        }

        previousRegistration.Dispose();
        var cancellationRegistration = cancellationToken.Register(
            () => CancelMicGatePublication(desiredVersion));
        var registrationIsCurrent = false;
        lock (_sync)
        {
            if (desiredVersion == _desiredMicGateVersion)
            {
                _micGateCancellationRegistration = cancellationRegistration;
                registrationIsCurrent = true;
            }
        }

        if (!registrationIsCurrent)
        {
            cancellationRegistration.Dispose();
        }

        await ReconcileMicGateAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private void CancelMicGatePublication(long desiredVersion)
    {
        var restoreOpenState = false;
        lock (_sync)
        {
            if (desiredVersion == _desiredMicGateVersion)
            {
                _micGateCancellationRegistration = default;
                _desiredMicGateEnabled = true;
                _desiredMicGateVersion++;
                restoreOpenState = true;
            }
        }

        if (restoreOpenState)
        {
            _ = RestoreOpenMicGateAfterCancellationAsync();
        }
    }

    private async Task RestoreOpenMicGateAfterCancellationAsync()
    {
        try
        {
            await ReconcileMicGateAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger?.LogDebug(exception, "Failed to restore the open microphone gate after cancellation.");
        }
    }

    private async Task ReconcileMicGateAsync(
        string? unpublishedSessionId = null,
        CancellationToken cancellationToken = default)
    {
        var terminalFailure = await WriteMicGateAsync(unpublishedSessionId, cancellationToken).ConfigureAwait(false);
        if (!terminalFailure.HasValue)
        {
            return;
        }

        try
        {
            await terminalFailure.Value.ReleaseTask.ConfigureAwait(false);
        }
        catch (Exception releaseException)
        {
            throw new AggregateException(terminalFailure.Value.GateException, releaseException);
        }

        System.Runtime.ExceptionServices.ExceptionDispatchInfo
            .Capture(terminalFailure.Value.GateException)
            .Throw();
        throw new InvalidOperationException("Unreachable after rethrowing the microphone gate failure.");
    }

    private async Task<(Exception GateException, Task ReleaseTask)?> WriteMicGateAsync(
        string? unpublishedSessionId,
        CancellationToken cancellationToken)
    {
        int? unpublishedLifecycleVersion = null;
        if (unpublishedSessionId is not null)
        {
            lock (_sync)
            {
                unpublishedLifecycleVersion = _lifecycleVersion;
            }
        }

        await _micGateWriter.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            while (true)
            {
                string? sessionId;
                int lifecycleVersion;
                bool desiredEnabled;
                long desiredVersion;
                lock (_sync)
                {
                    if (unpublishedLifecycleVersion.HasValue
                        && unpublishedLifecycleVersion.Value != _lifecycleVersion)
                    {
                        return null;
                    }

                    sessionId = unpublishedSessionId ?? _sessionId;
                    lifecycleVersion = _lifecycleVersion;
                    desiredEnabled = _desiredMicGateEnabled;
                    desiredVersion = _desiredMicGateVersion;

                    if (_appliedMicGateLifecycleVersion == lifecycleVersion
                        && _appliedMicGateSessionId == sessionId
                        && _appliedMicGateDesiredVersion == desiredVersion)
                    {
                        return null;
                    }
                }

                if (desiredVersion == 0)
                {
                    return null;
                }

                if (sessionId is null)
                {
                    return null;
                }

                try
                {
                    await _browser
                        .SetMicGateAsync(sessionId, desiredEnabled, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception gateException)
                {
                    var stillCurrent = false;
                    lock (_sync)
                    {
                        stillCurrent = StillOwnsMicGateWriteLocked(
                            lifecycleVersion,
                            desiredVersion,
                            unpublishedSessionId,
                            sessionId);
                        if (stillCurrent && unpublishedSessionId is null)
                        {
                            _quarantinedSessionId = sessionId;
                        }
                    }

                    if (!stillCurrent)
                    {
                        continue;
                    }

                    if (unpublishedSessionId is not null)
                    {
                        throw;
                    }

                    var releaseTask = ReleaseAsync(CancellationToken.None);
                    return (gateException, releaseTask);
                }

                lock (_sync)
                {
                    if (StillOwnsMicGateWriteLocked(
                            lifecycleVersion,
                            desiredVersion,
                            unpublishedSessionId,
                            sessionId))
                    {
                        _appliedMicGateLifecycleVersion = lifecycleVersion;
                        _appliedMicGateSessionId = sessionId;
                        _appliedMicGateDesiredVersion = desiredVersion;
                        return null;
                    }
                }
            }
        }
        finally
        {
            _micGateWriter.Release();
        }
    }

    public async ValueTask<CaptureTrackProcessing> ReadAsync(CancellationToken cancellationToken)
    {
        string? sessionId;
        lock (_sync)
        {
            sessionId = _sessionId;
        }

        if (sessionId is null)
        {
            return CaptureTrackProcessing.Unknown;
        }

        try
        {
            var dto = await _browser
                .ReadTrackProcessingAsync(sessionId, cancellationToken)
                .ConfigureAwait(false);
            return new CaptureTrackProcessing(
                dto.EchoCancellation,
                dto.NoiseSuppression,
                dto.AutoGainControl);
        }
        catch (JSException)
        {
            return CaptureTrackProcessing.Unknown;
        }
        catch (JSDisconnectedException)
        {
            return CaptureTrackProcessing.Unknown;
        }
        catch (ObjectDisposedException)
        {
            return CaptureTrackProcessing.Unknown;
        }
    }

    internal Task StopAsync(CancellationToken cancellationToken)
    {
        MicrophoneRecording? recording;
        lock (_sync)
        {
            recording = _recording;
        }

        return recording is null
            ? Task.CompletedTask
            : StopRecordingAsync(recording, cancellationToken);
    }

    private async Task StopRecordingAsync(MicrophoneRecording recording, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        Task? releaseTask;
        Task? activeStopTask;
        TaskCompletionSource? stopCompletion;
        Task<string>? acquisition;
        string? sessionId;
        int lifecycleVersion;

        lock (_sync)
        {
            releaseTask = _releaseCompletion?.Task;
            activeStopTask = null;
            stopCompletion = null;
            if (releaseTask is not null)
            {
                acquisition = null;
                sessionId = null;
                lifecycleVersion = 0;
            }
            else if (_stopCompletion is not null)
            {
                activeStopTask = _stopCompletion.Task;
                acquisition = null;
                sessionId = null;
                lifecycleVersion = 0;
            }
            else
            {
                if (!ReferenceEquals(_recording, recording))
                {
                    recording.OnStopped();
                    return;
                }

                lifecycleVersion = ++_lifecycleVersion;
                _transitionInProgress = true;
                stopCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                _stopCompletion = stopCompletion;
                acquisition = _acquisition;
                sessionId = _sessionId;
            }
        }

        if (activeStopTask is not null)
        {
            await activeStopTask.ConfigureAwait(false);
            return;
        }

        if (releaseTask is not null)
        {
            try
            {
                await releaseTask.ConfigureAwait(false);
                return;
            }
            catch (Exception)
            {
                await StopRecordingAsync(recording, CancellationToken.None).ConfigureAwait(false);
                return;
            }
        }

        if (acquisition is not null)
        {
            try
            {
                sessionId = await acquisition.ConfigureAwait(false);
                lock (_sync)
                {
                    if (StillOwnsLifecycleLocked(lifecycleVersion))
                    {
                        _sessionId = sessionId;
                        if (ReferenceEquals(_acquisition, acquisition))
                        {
                            _acquisition = null;
                        }
                    }
                }
            }
            catch (Exception)
            {
                sessionId = null;
            }
        }

        var stopCompleted = false;
        Exception? stopFailure = null;
        try
        {
            if (sessionId is not null && lifecycleVersion == Volatile.Read(ref _lifecycleVersion))
            {
                await DisconnectSessionAsync(sessionId).ConfigureAwait(false);
            }

            stopCompleted = true;
        }
        catch (Exception exception)
        {
            stopFailure = exception;
            throw;
        }
        finally
        {
            lock (_sync)
            {
                if (StillOwnsLifecycleLocked(lifecycleVersion))
                {
                    if (stopCompleted)
                    {
                        _recording = null;
                        _recordingNeedsStopRetry = false;
                    }
                    else
                    {
                        _recordingNeedsStopRetry = true;
                    }

                    _transitionInProgress = false;
                }

                if (ReferenceEquals(_stopCompletion, stopCompletion))
                {
                    if (stopFailure is null)
                    {
                        stopCompletion?.TrySetResult();
                    }
                    else
                    {
                        stopCompletion?.TrySetException(stopFailure);
                    }

                    _stopCompletion = null;
                }
            }
        }

        if (stopCompleted)
        {
            recording.OnStopped();
        }
    }

    private Task<string> GetOrStartAcquisitionLocked()
    {
        if (_quarantinedAcquisitionCleanup is not null)
        {
            return _acquisition ??= FinishQuarantinedAcquisitionCleanupAndAcquireAsync(
                _quarantinedAcquisitionCleanup);
        }

        if (_quarantinedAcquisition is not null)
        {
            return _acquisition ??= ReleaseQuarantinedAcquisitionAndAcquireAsync(
                _quarantinedAcquisition);
        }

        if (_quarantinedSessionId is not null)
        {
            return _acquisition ??= ReleaseQuarantinedSessionAndAcquireAsync(_quarantinedSessionId);
        }

        return _acquisition ??= _sessionId is not null
            ? EnsureLiveSessionAsync(_sessionId)
            : AcquireSessionAsync();
    }

    private void QuarantineAcquisitionLocked(Task<string> acquisition)
    {
        _quarantinedAcquisition ??= acquisition;
        if (!ReferenceEquals(_quarantinedAcquisition, acquisition)
            || _quarantinedAcquisitionCleanup is not null)
        {
            return;
        }

        _quarantinedAcquisitionCleanup = CompleteQuarantinedAcquisitionCleanupAsync(acquisition);
    }

    private async Task CompleteQuarantinedAcquisitionCleanupAsync(Task<string> acquisition)
    {
        string sessionId;
        try
        {
            sessionId = await acquisition.ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            lock (_sync)
            {
                if (ReferenceEquals(_quarantinedAcquisition, acquisition))
                {
                    _quarantinedAcquisition = null;
                }
            }

            _logger?.LogDebug(exception, "Late microphone acquisition ended without a browser session.");
            return;
        }

        var ownsSession = false;
        lock (_sync)
        {
            if (ReferenceEquals(_quarantinedAcquisition, acquisition))
            {
                _quarantinedAcquisition = null;
                _sessionId = sessionId;
                _quarantinedSessionId = sessionId;
                ownsSession = true;
            }
        }

        if (!ownsSession)
        {
            return;
        }

        try
        {
            await ReleaseSessionAsync(sessionId).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger?.LogDebug(exception, "Late microphone session cleanup remains retryable.");
            return;
        }

        lock (_sync)
        {
            if (_sessionId == sessionId)
            {
                _sessionId = null;
            }

            if (_quarantinedSessionId == sessionId)
            {
                _quarantinedSessionId = null;
            }

            _lastReleaseTimestamp = _timeProvider.GetTimestamp();
        }
    }

    private async Task<string> FinishQuarantinedAcquisitionCleanupAndAcquireAsync(Task cleanup)
    {
        try
        {
            await cleanup
                .WaitAsync(_releaseProgressTimeout, _timeProvider)
                .ConfigureAwait(false);
        }
        catch (TimeoutException timeout)
        {
            throw new MicrophoneAcquisitionException(
                MicrophoneAcquisitionFailureCode.Unavailable,
                timeout);
        }

        string? quarantinedSessionId;
        lock (_sync)
        {
            if (ReferenceEquals(_quarantinedAcquisitionCleanup, cleanup))
            {
                _quarantinedAcquisitionCleanup = null;
            }

            quarantinedSessionId = _quarantinedSessionId;
        }

        return quarantinedSessionId is not null
            ? await ReleaseQuarantinedSessionAndAcquireAsync(quarantinedSessionId).ConfigureAwait(false)
            : await AcquireSessionAsync().ConfigureAwait(false);
    }

    private async Task<string> ReleaseQuarantinedAcquisitionAndAcquireAsync(
        Task<string> acquisition)
    {
        string sessionId;
        try
        {
            sessionId = await acquisition
                .WaitAsync(_releaseProgressTimeout, _timeProvider)
                .ConfigureAwait(false);
        }
        catch (TimeoutException timeout)
        {
            throw new MicrophoneAcquisitionException(
                MicrophoneAcquisitionFailureCode.Unavailable,
                timeout);
        }
        catch
        {
            lock (_sync)
            {
                if (ReferenceEquals(_quarantinedAcquisition, acquisition))
                {
                    _quarantinedAcquisition = null;
                }
            }

            throw;
        }

        lock (_sync)
        {
            if (ReferenceEquals(_quarantinedAcquisition, acquisition))
            {
                _quarantinedAcquisition = null;
            }

            _sessionId = sessionId;
            _quarantinedSessionId = sessionId;
        }

        return await ReleaseQuarantinedSessionAndAcquireAsync(sessionId).ConfigureAwait(false);
    }

    private async Task<string> ReleaseQuarantinedSessionAndAcquireAsync(string sessionId)
    {
        try
        {
            await ReleaseSessionAsync(sessionId).ConfigureAwait(false);
        }
        catch (Exception releaseFailure)
        {
            throw new MicrophoneAcquisitionException(
                MicrophoneAcquisitionFailureCode.Unavailable,
                releaseFailure);
        }

        lock (_sync)
        {
            if (_quarantinedSessionId == sessionId)
            {
                _quarantinedSessionId = null;
            }

            if (_sessionId == sessionId)
            {
                _sessionId = null;
            }

            _lastReleaseTimestamp = _timeProvider.GetTimestamp();
        }

        return await AcquireSessionAsync().ConfigureAwait(false);
    }

    private async Task<string> EnsureLiveSessionAsync(string sessionId)
    {
        bool isLive;
        try
        {
            isLive = await _browser.IsLiveAsync(sessionId, CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch (Exception failure)
        {
            throw new MicrophoneAcquisitionException(
                MicrophoneAcquisitionFailureCode.Unavailable,
                failure);
        }

        if (isLive)
        {
            return sessionId;
        }

        lock (_sync)
        {
            _quarantinedSessionId = sessionId;
        }

        return await ReleaseQuarantinedSessionAndAcquireAsync(sessionId).ConfigureAwait(false);
    }

    private async Task<string> AcquireSessionAsync()
    {
        MicrophoneAcquisitionDto acquisition;
        try
        {
            acquisition = await _browser
                .AcquireAsync(CaptureKeepAliveEnabled, CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch (Exception failure)
        {
            throw new MicrophoneAcquisitionException(
                MicrophoneAcquisitionFailureCode.Unavailable,
                failure);
        }

        if (acquisition is null)
        {
            throw CreateMalformedAcquisitionFailure();
        }

        var hasSession = acquisition.SessionId is not null;
        var sessionId = acquisition.SessionId;
        var hasErrorValue = acquisition.ErrorName is not null;
        var hasValidFailure = !hasSession && !string.IsNullOrWhiteSpace(acquisition.ErrorName);
        var hasValidSession = hasSession && !string.IsNullOrEmpty(sessionId) && !hasErrorValue;
        if (!hasValidSession && !hasValidFailure)
        {
            var malformedFailure = CreateMalformedAcquisitionFailure();
            if (!string.IsNullOrEmpty(sessionId))
            {
                try
                {
                    await ReleaseMalformedSessionAsync(sessionId).ConfigureAwait(false);
                }
                catch (Exception releaseFailure)
                {
                    throw new MicrophoneAcquisitionException(
                        MicrophoneAcquisitionFailureCode.Unavailable,
                        new AggregateException(malformedFailure, releaseFailure));
                }
            }

            throw malformedFailure;
        }

        if (hasValidFailure)
        {
            throw CreateAcquisitionFailure(acquisition);
        }

        try
        {
            await ReconcileMicGateAsync(sessionId).ConfigureAwait(false);
        }
        catch (Exception gateFailure)
        {
            lock (_sync)
            {
                _quarantinedSessionId = sessionId;
            }

            try
            {
                await ReleaseSessionAsync(sessionId).ConfigureAwait(false);
            }
            catch (Exception releaseFailure)
            {
                throw new MicrophoneAcquisitionException(
                    MicrophoneAcquisitionFailureCode.Unavailable,
                    new AggregateException(gateFailure, releaseFailure));
            }

            lock (_sync)
            {
                if (_quarantinedSessionId == sessionId)
                {
                    _quarantinedSessionId = null;
                    _lastReleaseTimestamp = _timeProvider.GetTimestamp();
                }
            }

            throw new MicrophoneAcquisitionException(
                MicrophoneAcquisitionFailureCode.Unavailable,
                gateFailure);
        }

        long? lastReleaseTimestamp;
        lock (_sync)
        {
            lastReleaseTimestamp = _lastReleaseTimestamp;
        }

        if (lastReleaseTimestamp.HasValue)
        {
            var elapsed = _timeProvider.GetElapsedTime(lastReleaseTimestamp.Value, _timeProvider.GetTimestamp());
            _logger?.LogInformation(
                "Microphone device reclaimed after {FullyReleasedMilliseconds} ms fully released.",
                Math.Round(elapsed.TotalMilliseconds));
        }

        return sessionId;
    }

    private async Task ReleaseMalformedSessionAsync(string sessionId)
    {
        lock (_sync)
        {
            _quarantinedSessionId = sessionId;
        }

        await ReleaseSessionAsync(sessionId).ConfigureAwait(false);

        lock (_sync)
        {
            if (_quarantinedSessionId == sessionId)
            {
                _quarantinedSessionId = null;
                _lastReleaseTimestamp = _timeProvider.GetTimestamp();
            }
        }
    }

    private static MicrophoneAcquisitionException CreateAcquisitionFailure(MicrophoneAcquisitionDto acquisition)
    {
        var code = acquisition.ErrorName switch
        {
            "NotFoundError" or "DevicesNotFoundError" or "OverconstrainedError"
                => MicrophoneAcquisitionFailureCode.DeviceNotFound,
            "NotAllowedError" or "PermissionDeniedError" or "SecurityError"
                => MicrophoneAcquisitionFailureCode.PermissionDenied,
            _ => MicrophoneAcquisitionFailureCode.Unavailable
        };

        return new MicrophoneAcquisitionException(code);
    }

    private static MicrophoneAcquisitionException CreateMalformedAcquisitionFailure() =>
        new(
            MicrophoneAcquisitionFailureCode.Unavailable,
            new InvalidOperationException("The microphone acquisition result is malformed."));

    private async Task<Exception> ReleaseAfterFailedConnectionAsync(string sessionId, Exception connectionFailure)
    {
        lock (_sync)
        {
            _quarantinedSessionId = sessionId;
        }

        try
        {
            await ReleaseSessionAsync(sessionId).ConfigureAwait(false);
        }
        catch (Exception cleanupFailure)
        {
            return new AggregateException(
                "Microphone connection failed and the browser session could not be released.",
                connectionFailure,
                cleanupFailure);
        }

        lock (_sync)
        {
            if (_sessionId == sessionId)
            {
                _sessionId = null;
            }

            if (_quarantinedSessionId == sessionId)
            {
                _quarantinedSessionId = null;
            }

            _lastReleaseTimestamp = _timeProvider.GetTimestamp();
        }

        return connectionFailure;
    }

    private async Task DisconnectSessionAsync(string sessionId)
    {
        try
        {
            await _browser.DisconnectAsync(sessionId, CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch (JSDisconnectedException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private async Task ReleaseSessionAsync(string sessionId)
    {
        try
        {
            await _browser.ReleaseAsync(sessionId, CancellationToken.None)
                .AsTask()
                .WaitAsync(_releaseProgressTimeout, _timeProvider)
                .ConfigureAwait(false);
        }
        catch (JSDisconnectedException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    /// <summary>
    /// Lehnt eine Aufnahme ab, deren gemeldete Abtastrate nicht die der Erfassungskette ist.
    /// </summary>
    /// <param name="mimeType">Der vom Browser gemeldete Inhaltstyp, etwa <c>audio/pcm;rate=16000</c>.</param>
    private static void AssertReportedSampleRate(string? mimeType)
    {
        // Der Ringspeicher rechnet Millisekunden gegen seine eigene Abtastrate. Stimmt die gemeldete Rate des Browsers
        // nicht mit ihr überein, würde die Dauer des Aufnahmefensters falsch berechnet.
        if (ReadSampleRateHz(mimeType) == RequiredSampleRateHz)
        {
            return;
        }

        throw new MicrophoneAcquisitionException(
            MicrophoneAcquisitionFailureCode.Unavailable,
            new InvalidOperationException(
                $"Microphone reported '{mimeType}'; capture requires {SampleRateParameter}{RequiredSampleRateHz}."));
    }

    /// <summary>
    /// Liest die Abtastrate aus den Parametern eines Inhaltstyps.
    /// </summary>
    /// <param name="mimeType">Der zu lesende Inhaltstyp. Darf <see langword="null"/> sein.</param>
    /// <returns>Die gelesene Rate in Hertz, oder <see langword="null"/>, wenn kein gültiger ganzzahliger Wert angegeben ist.</returns>
    private static int? ReadSampleRateHz(string? mimeType)
    {
        foreach (var parameter in (mimeType ?? string.Empty).Split(';', StringSplitOptions.TrimEntries))
        {
            if (parameter.StartsWith(SampleRateParameter, StringComparison.OrdinalIgnoreCase)
                && int.TryParse(parameter.AsSpan(SampleRateParameter.Length), out var hertz))
            {
                return hertz;
            }
        }

        return null;
    }

}
