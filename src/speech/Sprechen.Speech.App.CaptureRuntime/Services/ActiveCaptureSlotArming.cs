namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Verbindet Aufnahmezulassung und Segmenterkonfiguration mit dem aktiven Aufnahmeplatz,
/// damit Versuchsbeginn und spätere Abbruchsignale denselben Zustand verwenden.
/// </summary>
internal sealed class ActiveCaptureSlotArming
{
    private readonly SpeechCaptureSegmenterFactory _segmenterFactory;
    private readonly ActiveCaptureSlotStore _slotStore;
    private readonly ActiveCaptureTerminalSignal _terminalSignal;

    internal ActiveCaptureSlotArming(
        SpeechCaptureSegmenterFactory segmenterFactory,
        ActiveCaptureSlotStore slotStore,
        ActiveCaptureTerminalSignal terminalSignal)
    {
        _segmenterFactory = segmenterFactory ?? throw new ArgumentNullException(nameof(segmenterFactory));
        _slotStore = slotStore ?? throw new ArgumentNullException(nameof(slotStore));
        _terminalSignal = terminalSignal ?? throw new ArgumentNullException(nameof(terminalSignal));
    }

    internal ActiveCaptureSlot Arm(
        SpeechAudioMimeType mimeType,
        int silenceDebounceMs,
        SpeechCaptureStartContext captureStartContext,
        SpeechCaptureModeBehavior behavior,
        TimeSpan? initialSilenceTimeout,
        SampleCursor? startCursor,
        int speechAmplitudeThreshold,
        long attemptId)
    {
        var armedSegmenter = _segmenterFactory.CreateSegmenter(
            mimeType,
            silenceDebounceMs,
            captureStartContext,
            behavior,
            initialSilenceTimeout,
            speechAmplitudeThreshold);

        return _slotStore.Arm(armedSegmenter, behavior, startCursor, attemptId);
    }

    internal CaptureAttemptAdmission AdmitAttempt() => _slotStore.AdmitAttempt();

    internal long BeginAttempt(CaptureAttemptAdmission admission) => _slotStore.BeginAttempt(admission);

    internal void ThrowIfAdmissionInvalid(CaptureAttemptAdmission admission) =>
        _slotStore.ThrowIfAdmissionInvalid(admission);

    internal void EndAttempt(long attemptId) => _slotStore.EndAttempt(attemptId);

    internal void CancelSlot(ActiveCaptureSlot activeSlot, CancellationToken cancellationToken)
    {
        _terminalSignal.CancelSlot(activeSlot, cancellationToken);
    }

    internal void Clear(ActiveCaptureSlot activeSlot)
    {
        _slotStore.Clear(activeSlot);
    }

    internal void DisposeAdmissionCancellation() => _slotStore.DisposeAdmissionCancellation();
}
