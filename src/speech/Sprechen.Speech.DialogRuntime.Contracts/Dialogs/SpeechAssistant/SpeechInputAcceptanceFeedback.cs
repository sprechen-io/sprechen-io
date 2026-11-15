namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Bestimmt die Rückmeldung zur angenommenen Spracheingabe. Standarderfassung
/// verwendet ihre allgemeine Rückmeldung. Bei späterer Interpretation bestätigt
/// erst die inhaltliche Antwort die Eingabe. Geschützte Zeicheneingabe kann
/// auf diese Rückmeldung verzichten.
/// </summary>
public enum SpeechInputAcceptanceFeedback
{
    /// <summary>
    /// Die Aufnahme spielt ihre allgemeine Rückmeldung zur Annahme und Verarbeitung.
    /// </summary>
    Standard,

    /// <summary>
    /// Die Aufnahme selbst bleibt still. Erst die inhaltliche Antwort des Parsers bestätigt die Eingabe.
    /// </summary>
    PostInterpretation,

    /// <summary>
    /// Unterdrückt Annahme-, Verarbeitungs-, Vorlese- und Ersatzrückmeldungen für diese Eingabe.
    /// </summary>
    CueFree,
}
