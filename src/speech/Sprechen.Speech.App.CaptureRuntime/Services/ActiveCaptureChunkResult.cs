namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Verbindet Sprachbeginn, Aufnahmeverhalten und einen möglichen Abschluss,
/// damit der Blockablauf die passenden Signale außerhalb des Zustandsspeichers ausgeben kann.
/// </summary>
internal readonly record struct ActiveCaptureChunkResult(
    ActiveCaptureSlot? CompletedSlot,
    CapturedSpeechAudio? CompletedUtterance,
    Exception? CompletionException,
    bool SpeechStarted,
    SpeechCaptureModeBehavior ModeBehavior)
{
    internal static ActiveCaptureChunkResult NoActiveCapture { get; } = new(
        CompletedSlot: null,
        CompletedUtterance: null,
        CompletionException: null,
        SpeechStarted: false,
        ModeBehavior: default);
}
