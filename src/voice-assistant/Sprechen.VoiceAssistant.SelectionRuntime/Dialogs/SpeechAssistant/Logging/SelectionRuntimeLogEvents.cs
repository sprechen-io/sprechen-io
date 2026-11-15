using Microsoft.Extensions.Logging;

namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant.Logging;

/// <summary>
/// Bündelt Diagnosekennungen der Auswahllaufzeit im 2000er-Bereich, damit Erkennungs- und
/// Wiedergabeausgänge gezielt gefiltert werden können.
/// </summary>
internal static class SelectionRuntimeLogEvents
{
    public static readonly EventId SpeechRecognitionFailedDuringBargeIn = new(2001, nameof(SpeechRecognitionFailedDuringBargeIn));

    public static readonly EventId SpeechRecognitionFailedDuringListening = new(2002, nameof(SpeechRecognitionFailedDuringListening));

    public static readonly EventId PromptPlaybackFailed = new(2003, nameof(PromptPlaybackFailed));

    public static readonly EventId TtsFailed = new(2004, nameof(TtsFailed));

    public static readonly EventId SpeechToTextRejected = new(2005, nameof(SpeechToTextRejected));

    public static readonly EventId CapturedUtteranceRecognizedAsEmpty = new(2006, nameof(CapturedUtteranceRecognizedAsEmpty));

    /// <summary>
    /// Kennzeichnet ausgeschöpfte Wiederholungen nach leerer Texterkennung als Warnung.
    /// Der Zähler wird am Ende des Aufnahmeversuchs protokolliert. Die aktuelle
    /// Dialogphase entscheidet anhand des Ergebnisses über die folgende Rückmeldung.
    /// </summary>
    public static readonly EventId CapturedButEmptyReArmExhausted = new(2007, nameof(CapturedButEmptyReArmExhausted));

    /// <summary>
    /// Kennzeichnet einen nicht vom Aufrufer angeforderten Wiedergabeabbruch. Diese Grenze
    /// unterscheidet eine Nutzerunterbrechung nicht sicher von einem Abbruch des entfernten
    /// Wiedergabewegs.
    /// </summary>
    public static readonly EventId PromptPlaybackCancelledUnattributed =
        new(2008, nameof(PromptPlaybackCancelledUnattributed));
}
