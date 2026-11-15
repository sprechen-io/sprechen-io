namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Unterscheidet die Ergebnisse der Vertrauensbewertung, damit Aufrufer ihre
/// anschließende Behandlung wählen können. Der Wert führt selbst keine Bestätigung aus.
/// </summary>
internal enum SttConfidenceLevel
{
    /// <summary>
    /// Die gemeldete Sicherheit erreicht die geprüfte Annahmegrenze.
    /// </summary>
    Sufficient,

    /// <summary>
    /// Der Wert liegt im unsicheren Bereich, besitzt keine gemeldete Sicherheit
    /// oder wurde durch die zulässige Abschwächung diesem Bereich zugeordnet.
    /// </summary>
    Uncertain,

    /// <summary>
    /// Die gemeldete Sicherheit unterschreitet die geprüften Grenzen ohne
    /// Abschwächung. Diese Einstufung ist keine Zeitüberschreitung.
    /// </summary>
    Insufficient,
}
