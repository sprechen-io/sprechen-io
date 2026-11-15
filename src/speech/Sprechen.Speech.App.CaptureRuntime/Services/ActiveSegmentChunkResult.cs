namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Trägt Sprachbeginn und einen möglichen Segmentabschluss aus der Blockverarbeitung,
/// damit der Abschluss nach Freigabe des Zustandsspeichers signalisiert werden kann.
/// </summary>
internal readonly record struct ActiveSegmentChunkResult(
    ActiveSegmentSlot? CompletedSlot,
    CapturedSpeechAudio? CompletedUtterance,
    Exception? CompletionException,
    bool SpeechStarted)
{
    internal static ActiveSegmentChunkResult NoActiveSegment { get; } = new(
        CompletedSlot: null,
        CompletedUtterance: null,
        CompletionException: null,
        SpeechStarted: false);
}
