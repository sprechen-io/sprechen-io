namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Enthält sprachbezogene Ordinalwörter für Monatstage und englische Einerformen
/// für zusammengesetzte Ordinalzahlen. Deutsche Stämme und Endungen liegen
/// im gesonderten Datumseingabevokabular.
/// </summary>
internal sealed class FormOrdinalDayVocabulary
{
    // Ordinalwort auf Monatstag abbilden, etwa „eighth“ auf 8 oder „premier“ auf 1.
    public Dictionary<string, int> Days { get; init; } = new(StringComparer.Ordinal);

    // Ordinale Einerformen für Zusammensetzungen wie „twenty first“ getrennt halten,
    // damit der englische Parser sie gezielt mit Zehnerwörtern verbinden kann.
    public Dictionary<string, int> CompoundOnes { get; init; } = new(StringComparer.Ordinal);
}
