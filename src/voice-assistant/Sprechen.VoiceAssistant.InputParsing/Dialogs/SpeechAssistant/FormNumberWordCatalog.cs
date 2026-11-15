namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Stellt modellierte Zahlwortschreibweisen je Sprache für Erkennungshinweise bereit.
/// Die gemeinsame Lexikonquelle hält dieses Vokabular bei den Werttabellen der Parser.
/// Die Liste allein beschreibt nicht deren zulässige Zusammensetzungen.
/// </summary>
public static class FormNumberWordCatalog
{
    /// <summary>
    /// Liefert Wörter für Einer, Zahlen von 10 bis 19, Zehner und Größenordnungen
    /// für die angegebene Sprache in dieser Reihenfolge. Ohne Sprachtabelle bleibt die Liste leer.
    /// </summary>
    public static IReadOnlyList<string> GetNumberWords(string isoCode)
    {
        if (string.IsNullOrWhiteSpace(isoCode))
        {
            return [];
        }

        // Originalschreibweisen für Erkennungshinweise verwenden: Der normalisierte
        // Vergleichsschlüssel „funf“ ersetzt hier nicht das modellierte Wort „fünf“.
        var index = new FormTranscriptLexiconIndex(EmbeddedFormTranscriptLexiconProvider.Instance.GetLexicon());
        return index.NumberWordSpellingsByLanguage.TryGetValue(isoCode.Trim(), out var spellings)
            ? spellings
            : [];
    }
}
