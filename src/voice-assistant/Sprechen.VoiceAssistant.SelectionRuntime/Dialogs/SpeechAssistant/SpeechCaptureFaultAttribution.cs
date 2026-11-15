namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Unterscheidet Fehlerkategorien für Wiederholungsgrenzen und Rückmeldungen.
/// </summary>
public enum SpeechCaptureFaultAttribution
{
    /// <summary>
    /// Kennzeichnet einen Fehler, der anhand der Regeln der Person oder ihrer
    /// Umgebung zugeordnet wird.
    /// </summary>
    Participant = 0,

    /// <summary>
    /// Kennzeichnet einen Fehler der technischen Infrastruktur bei Aufnahme oder
    /// Erkennung und grenzt ihn von personenbezogenen Fehlerkategorien ab.
    /// </summary>
    Infrastructure = 1,
}
