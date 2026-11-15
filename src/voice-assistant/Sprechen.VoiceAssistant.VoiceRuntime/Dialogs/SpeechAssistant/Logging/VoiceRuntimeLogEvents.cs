using Microsoft.Extensions.Logging;

namespace Sprechen.VoiceAssistant.VoiceRuntime.Dialogs.SpeechAssistant.Logging;

/// <summary>
/// Benennt technische Ereignisse der Formular-Sprachlaufzeit mit stabilen
/// Kennungen, damit ihre Einordnung nicht vom Meldungstext abhängt.
/// </summary>
internal static class VoiceRuntimeLogEvents
{
    public static readonly EventId TtsFailed = new(1001, nameof(TtsFailed));

    public static readonly EventId PromptPlaybackFailed = new(1002, nameof(PromptPlaybackFailed));

    /// <summary>
    /// Die Ausgabe wurde abgebrochen, ohne dass der hier übergebene Abbruch
    /// signalisiert war. Der Protokolleintrag hält die Zuordnung offen.
    /// </summary>
    public static readonly EventId PromptPlaybackCancelledUnattributed =
        new(1007, nameof(PromptPlaybackCancelledUnattributed));

    public static readonly EventId SpeechToTextFailed = new(1003, nameof(SpeechToTextFailed));

    public static readonly EventId SpeechToTextRejectedBackoffExhausted = new(1004, nameof(SpeechToTextRejectedBackoffExhausted));

    public static readonly EventId SpeechToTextRejectedWorkerQueueFull = new(1006, nameof(SpeechToTextRejectedWorkerQueueFull));
}
