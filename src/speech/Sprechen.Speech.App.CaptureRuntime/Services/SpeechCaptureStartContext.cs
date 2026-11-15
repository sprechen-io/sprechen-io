namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Unterscheidet Aufnahmebeginn nach oder während eigener Wiedergabe von ungestörtem Hören,
/// damit die Segmentierung den jeweils nötigen Schutz vor Ausgaberesten wählen kann.
/// </summary>
internal enum SpeechCaptureStartContext
{
    CleanListening = 0,
    AssistantPlaybackRecentlySettledIdle = 1,
    AssistantPlaybackRecentlySettledContinuation = 2,
    AssistantPlaybackActive = 3
}
