namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Grund für das Scheitern des vorangehenden Erfassungsversuchs.
/// <see cref="FormVoiceCommandAttempt"/> überträgt ihn, damit die gesprochene
/// Nachfrage zum jeweiligen Fehler passt.
/// </summary>
public enum FormVoiceCommandAttemptFailure
{
    /// <summary>Das ist der erste Versuch. Kein vorangehender Versuch scheiterte.</summary>
    None = 0,

    /// <summary>Der vorangehende Versuch erkannte keine Sprache vor seinem Leerlauf-Timeout.</summary>
    NoInputTimeout = 1,

    /// <summary>
    /// Die vorherige Aufnahme scheiterte. Dieser Wert ist für eine Wiederholung nach
    /// Aufnahmefehlern vorgesehen. Der Aufnahmedienst sendet ihn derzeit nicht, da er nur
    /// bei ausbleibender Eingabe wiederholt. Verbraucher dürfen seinen Empfang nicht voraussetzen.
    /// </summary>
    CaptureFailure = 2,

    /// <summary>
    /// Die vorherige Aufnahme lieferte eine im aktuellen Dialogschritt unpassende Antwort,
    /// etwa weder Ja noch Nein auf eine Bestätigungsfrage. Der Bestätigungsablauf meldet
    /// diesen Grund bei einer erneuten Nachfrage.
    /// </summary>
    UnrecognizedAnswer = 3,
}
