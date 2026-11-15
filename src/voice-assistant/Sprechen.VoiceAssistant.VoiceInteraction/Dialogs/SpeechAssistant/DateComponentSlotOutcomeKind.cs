namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Unterscheidet die Ergebnisse beim Sammeln eines benannten Datumsbestandteils.
/// </summary>
internal enum DateComponentSlotOutcomeKind
{
    Collected,

    /// <summary>
    /// Kennzeichnet den Abbruch des Datumsdialogs zur normalen Feldeingabe.
    /// </summary>
    Aborted,

    /// <summary>
    /// Kennzeichnet die Übernahme eines vollständigen Datums statt weiterer Bestandteile.
    /// </summary>
    Escaped
}
