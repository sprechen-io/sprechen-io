namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Verbindet den Segmentierer mit dem wartenden Aufrufer und der Abbruchregistrierung,
/// damit Erfolg, Fehler und Abbruch dieselbe Segmentaufnahme abschließen.
/// </summary>
internal sealed class ActiveSegmentSlot : IArmedCaptureWindow
{
    private readonly TaskCompletionSource<CapturedSpeechAudio> _capture = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly PcmSpeechCaptureSegmenter _segmenter;
    private CancellationTokenRegistration _cancellationRegistration;
    private bool _cancellationRegistered;

    internal ActiveSegmentSlot(PcmSpeechCaptureSegmenter segmenter)
    {
        _segmenter = segmenter ?? throw new ArgumentNullException(nameof(segmenter));
    }

    internal Task<CapturedSpeechAudio> CaptureTask => _capture.Task;

    /// <summary>Höchste pro Frame beobachtete Mittelamplitude für den Aufnahmebericht.</summary>
    public int MaxAmplitudeObserved => _segmenter.PeakLevel.Observed;

    public int ObservedFrameCount => _segmenter.PeakLevel.ObservedFrames;

    /// <summary>Zuletzt geschätzter Hintergrundpegel für die Erkennung des Sprachbeginns.</summary>
    public int EstimatedFloor => _segmenter.PeakLevel.EstimatedFloor;

    /// <summary>Zuletzt verwendete Schwelle zur Erkennung des Sprachbeginns.</summary>
    public int AppliedOnsetThreshold => _segmenter.PeakLevel.AppliedOnsetThreshold;

    internal void RegisterCancellation(
        CancellationToken cancellationToken,
        Action<ActiveSegmentSlot, CancellationToken> cancelSlot)
    {
        ArgumentNullException.ThrowIfNull(cancelSlot);

        if (_cancellationRegistered)
        {
            throw new InvalidOperationException("A active segment slot can only register cancellation once.");
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
