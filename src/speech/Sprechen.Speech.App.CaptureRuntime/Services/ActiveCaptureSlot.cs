namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Hält den Segmentierer und den Abschluss eines laufenden Aufnahmeversuchs zusammen,
/// damit der Bericht dessen Amplitude und Framezahl vor der Freigabe lesen kann.
/// </summary>
internal sealed class ActiveCaptureSlot : IArmedCaptureWindow
{
    private readonly TaskCompletionSource<CapturedSpeechAudio> _capture = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly PcmSpeechCaptureSegmenter _segmenter;
    private CancellationTokenRegistration _cancellationRegistration;
    private bool _cancellationRegistered;

    internal ActiveCaptureSlot(PcmSpeechCaptureSegmenter segmenter, SpeechCaptureModeBehavior modeBehavior)
    {
        _segmenter = segmenter ?? throw new ArgumentNullException(nameof(segmenter));
        ModeBehavior = modeBehavior;
    }

    internal Task<CapturedSpeechAudio> CaptureTask => _capture.Task;

    internal SpeechCaptureModeBehavior ModeBehavior { get; }

    /// <summary>Höchste pro Frame beobachtete Mittelamplitude für den Aufnahmebericht.</summary>
    public int MaxAmplitudeObserved => _segmenter.PeakLevel.Observed;

    public int ObservedFrameCount => _segmenter.PeakLevel.ObservedFrames;

    /// <summary>Zuletzt geschätzter Hintergrundpegel für die Erkennung des Sprachbeginns.</summary>
    public int EstimatedFloor => _segmenter.PeakLevel.EstimatedFloor;

    /// <summary>Zuletzt verwendete Schwelle zur Erkennung des Sprachbeginns.</summary>
    public int AppliedOnsetThreshold => _segmenter.PeakLevel.AppliedOnsetThreshold;

    internal void RegisterCancellation(
        CancellationToken cancellationToken,
        Action<ActiveCaptureSlot, CancellationToken> cancelSlot)
    {
        ArgumentNullException.ThrowIfNull(cancelSlot);

        if (_cancellationRegistered)
        {
            throw new InvalidOperationException("An active capture slot can only register cancellation once.");
        }

        _cancellationRegistered = true;
        _cancellationRegistration = cancellationToken.Register(() => cancelSlot(this, cancellationToken));
    }

    internal PcmSpeechCaptureSegmenterUpdate ReadFrom(SpeechSampleRing sampleRing, ref SampleCursor cursor)
    {
        return _segmenter.ReadFrom(sampleRing, ref cursor);
    }

    internal CapturedSpeechAudio? FlushPartial()
    {
        return _segmenter.FlushPartial();
    }

    internal bool TryComplete(CapturedSpeechAudio utterance)
    {
        ArgumentNullException.ThrowIfNull(utterance);

        return _capture.TrySetResult(utterance);
    }

    internal bool TryFail(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return _capture.TrySetException(exception);
    }

    internal bool TryCancel(CancellationToken cancellationToken)
    {
        return _capture.TrySetCanceled(cancellationToken);
    }

    public void Dispose()
    {
        _cancellationRegistration.Dispose();
    }
}
