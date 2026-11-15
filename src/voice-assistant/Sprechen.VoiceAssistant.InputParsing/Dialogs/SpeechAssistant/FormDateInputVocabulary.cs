namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Enthält sprachbezogene Wörter und Ordinalstämme für die Datumseingabe. Der
/// Lexikonindex leitet Vergleichsschlüssel für Tagesabstände und Stämme ab.
/// Monatsnamen bleiben beim kulturabhängigen Monatsindex.
/// </summary>
internal sealed class FormDateInputVocabulary
{
    // Relatives Tageswort auf den Abstand zu heute abbilden, etwa „gestern“ auf -1.
    public Dictionary<string, int> RelativeDayOffsets { get; init; } = new(StringComparer.Ordinal);

    // Optionale Partikel vor dem Tag, etwa „am“ in „am dritten Mai“.
    public string[] LeadingParticles { get; init; } = [];

    // Vor der Stammsuche entfernte Ordinalendungen, längere Endungen zuerst.
    public string[] OrdinalEndings { get; init; } = [];

    // Ordinalstamm auf den Monatstag abbilden, etwa „dritt“ auf 3.
    public Dictionary<string, int> OrdinalDayStems { get; init; } = new(StringComparer.Ordinal);
}
