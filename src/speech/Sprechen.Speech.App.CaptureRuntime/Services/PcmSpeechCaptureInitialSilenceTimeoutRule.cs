namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Überführt die erreichte Anfangsstillegrenze in ein Ergebnis ohne erkannte Sprache,
/// damit die Segmentierung auch ohne bestätigten Sprachbeginn einen Abschluss liefern kann.
/// </summary>
internal sealed class PcmSpeechCaptureInitialSilenceTimeoutRule
{
    private readonly PcmSpeechCaptureSegmenterOptions _options;
    private readonly PcmSpeechCaptureProgressState _progressState;

    internal PcmSpeechCaptureInitialSilenceTimeoutRule(
        PcmSpeechCaptureSegmenterOptions options,
        PcmSpeechCaptureProgressState progressState)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _progressState = progressState ?? throw new ArgumentNullException(nameof(progressState));
    }

    internal PcmSpeechCaptureSegmenterUpdate BuildUpdate()
    {
        return _progressState.HasReachedInitialSilenceTimeout(_options.MaxInitialSilence.TotalMilliseconds)
            ? new PcmSpeechCaptureSegmenterUpdate(false, null, new NoSpeechDetectedException())
            : default;
    }
}
