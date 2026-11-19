namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Koordiniert Aktivierung, Pufferauswertung und Überwachung eines Aufnahmeversuchs,
/// damit Audiodaten und Abschluss gemeinsam verarbeitet werden.
/// </summary>
internal sealed class ActiveCaptureAttemptFlow : IDisposable
{
    private readonly ActiveCaptureMicGateGuard _micGateGuard;
    private readonly ActiveCaptureTrace _trace;
    private readonly ActiveCaptureSlotArming _slotArming;
    private readonly ActiveCaptureCursorBacklogDrainer _cursorBacklogDrainer;
    private readonly CaptureChunkLivenessGuard _chunkLiveness;
    private readonly CaptureTerminalFlow _terminalFlow;

    internal ActiveCaptureAttemptFlow(
        ActiveCaptureMicGateGuard micGateGuard,
        ActiveCaptureTrace trace,
        ActiveCaptureSlotArming slotArming,
        ActiveCaptureCursorBacklogDrainer cursorBacklogDrainer,
        CaptureChunkLivenessGuard chunkLiveness)
    {
        _micGateGuard = micGateGuard ?? throw new ArgumentNullException(nameof(micGateGuard));
        _trace = trace ?? throw new ArgumentNullException(nameof(trace));
        _slotArming = slotArming ?? throw new ArgumentNullException(nameof(slotArming));
        _cursorBacklogDrainer = cursorBacklogDrainer ?? throw new ArgumentNullException(nameof(cursorBacklogDrainer));
        _chunkLiveness = chunkLiveness ?? throw new ArgumentNullException(nameof(chunkLiveness));
        _terminalFlow = new CaptureTerminalFlow(trace);
    }

    internal Task<CapturedSpeechAudio> CaptureAsync(
        AdmittedCaptureAttempt admittedAttempt,
        SpeechAudioMimeType mimeType,
        int silenceDebounceMs,
        SpeechCaptureStartContext captureStartContext,
        SpeechCaptureModeBehavior behavior,
        TimeSpan? initialSilenceTimeout,
        SampleCursor? startCursor,
        CancellationToken cancellationToken)
        => _terminalFlow.RunAsync(
            () => CaptureAdmittedAsync(
                admittedAttempt,
                mimeType,
                silenceDebounceMs,
                captureStartContext,
                behavior,
                initialSilenceTimeout,
                startCursor,
                cancellationToken),
            cancellationToken);

    private async Task<CapturedSpeechAudio> CaptureAdmittedAsync(
        AdmittedCaptureAttempt admittedAttempt,
        SpeechAudioMimeType mimeType,
        int silenceDebounceMs,
        SpeechCaptureStartContext captureStartContext,
        SpeechCaptureModeBehavior behavior,
        TimeSpan? initialSilenceTimeout,
        SampleCursor? startCursor,
        CancellationToken cancellationToken)
    {
        // Ordnet dem Versuch zuerst Modus und Startkontext zu, auch
        // wenn er vor dem Aktivieren der Aufnahme abbricht.
        await _trace.RecordStartAsync(silenceDebounceMs, captureStartContext, behavior).ConfigureAwait(false);
        var attemptId = _slotArming.BeginAttempt(admittedAttempt.Admission);
        try
        {
            await _micGateGuard.WaitUntilCanStartAsync(cancellationToken).ConfigureAwait(false);

            ActiveCaptureSlot? armedSlot = null;
            try
            {
                await _trace.RecordCursorHandoffAsync(startCursor).ConfigureAwait(false);

                armedSlot = _slotArming.Arm(
                    mimeType,
                    silenceDebounceMs,
                    captureStartContext,
                    behavior,
                    initialSilenceTimeout,
                    startCursor,
                    admittedAttempt.SpeechAmplitudeThreshold,
                    attemptId);
                var activeSlot = armedSlot;

                if (startCursor is not null)
                {
                    var lookbackBytes = await _cursorBacklogDrainer.DrainAsync(activeSlot, cancellationToken)
                        .ConfigureAwait(false);
                    _trace.ObserveLookbackBytes(lookbackBytes);
                }

                activeSlot.RegisterCancellation(cancellationToken, _slotArming.CancelSlot);
                await _trace.RecordArmedAsync().ConfigureAwait(false);

                var captured = await _chunkLiveness
                    .AwaitAsync(activeSlot.CaptureTask, activeSlot.TryFail, cancellationToken)
                    .ConfigureAwait(false);
                return await _terminalFlow.CompleteAsync(activeSlot, captured).ConfigureAwait(false);
            }
            finally
            {
                _terminalFlow.Release(armedSlot, slot => _slotArming.Clear(slot));
            }
        }
        finally
        {
            _slotArming.EndAttempt(attemptId);
        }
    }

    public void Dispose()
    {
        _slotArming.DisposeAdmissionCancellation();
        _terminalFlow.Dispose();
    }

    internal CaptureAttemptAdmission AdmitAttempt() => _slotArming.AdmitAttempt();

    internal void ThrowIfAdmissionInvalid(CaptureAttemptAdmission admission) =>
        _slotArming.ThrowIfAdmissionInvalid(admission);
}
