namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Nimmt laufende Aufgabe, Sitzungstoken und Aufnahme gemeinsam aus dem Zustand mit,
/// damit deren Bereinigung auch nach dem Zurücksetzen des Sitzungsspeichers möglich bleibt.
/// </summary>
internal readonly record struct SpeechCaptureSessionSnapshot(
    Task? SessionTask,
    CancellationTokenSource? SessionCts,
    IMicrophoneRecording? Recording,
    SpeechAudioMimeType MicrophoneMimeType)
{
    internal bool HasSession => SessionTask is not null || SessionCts is not null || Recording is not null;
}
