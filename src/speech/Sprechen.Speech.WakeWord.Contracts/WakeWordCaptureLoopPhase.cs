namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Unterscheidet Wiederholungen nach Aufnahme- und Transkriptionsproblemen, damit die Telemetrie deren Ursache nicht vermischt.
/// </summary>
public enum WakeWordCaptureLoopPhase
{
    CaptureNoSpeechRetry = 0,
    CaptureFailedRetry = 1,
    TranscriptionNoSpeechRetry = 2,
    TranscriptionEmptyRetry = 3
}
