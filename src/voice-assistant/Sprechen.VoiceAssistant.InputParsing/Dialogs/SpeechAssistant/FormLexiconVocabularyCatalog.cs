namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Stellt Erkennungsvokabular aus demselben Transkriptlexikon bereit, das die
/// Eingabeparser verwenden, damit Erkennungshinweise aus den modellierten Wörtern entstehen.
/// </summary>
public static class FormLexiconVocabularyCatalog
{
    /// <summary>
    /// Liefert alle Buchstabiertokens in ihrer Modellschreibweise und Lexikonreihenfolge.
    /// Die sprachübergreifende Tabelle enthält Alphabetwörter und Symbolnamen.
    /// Normalisierte Vergleichsschlüssel ersetzen die Originalschreibweisen hier nicht.
    /// </summary>
    public static IReadOnlyList<string> GetSpellingTokens()
        => Index().SpellingTokenSpellings;

    /// <summary>
    /// Liefert modellierte Erkennungshinweise für das Buchstabieren in
    /// <paramref name="isoCode"/> samt einzelnen lateinischen Buchstaben.
    /// Alphabetwörter werden über das Sprachformat gewählt. Hinzu kommen Einerwörter,
    /// Symbol- und Nicht-ASCII-Aliase, Bindewörter und Großschreibungsmarker.
    /// Fehlt eine benötigte Sprachtabelle, bleibt die Liste leer.
    /// </summary>
    public static IReadOnlyList<string> GetSpellingRecognitionTerms(string isoCode)
    {
        if (string.IsNullOrWhiteSpace(isoCode))
        {
            return [];
        }

        var normalizedIsoCode = isoCode.Trim();
        var lexicon = EmbeddedFormTranscriptLexiconProvider.Instance.GetLexicon();
        var index = new FormTranscriptLexiconIndex(lexicon);
        if (!index.TryGetSpokenSpellingFormat(normalizedIsoCode, out var format)
            || !index.TryGetSpellingAlphabet(format.Alphabet, out var alphabet)
            || !lexicon.NumberWordsByLanguage.TryGetValue(normalizedIsoCode, out var numberWords)
            || !lexicon.SpellingConnectorsByLanguage.TryGetValue(normalizedIsoCode, out var connectors)
            || !lexicon.UppercaseMarkersByLanguage.TryGetValue(normalizedIsoCode, out var uppercaseMarkers))
        {
            return [];
        }

        var terms = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AddDistinct(terms, seen, alphabet.Values);
        AddDistinct(
            terms,
            seen,
            Enumerable.Range('a', 26).Select(code => ((char)code).ToString()));
        AddDistinct(terms, seen, numberWords.Units.Keys);
        AddDistinct(
            terms,
            seen,
            lexicon.SpellingTokens
                .Where(entry => IsNonAsciiLetterOrSymbol(entry.Value))
                .Select(entry => entry.Key));
        AddDistinct(terms, seen, connectors);
        AddDistinct(terms, seen, uppercaseMarkers);

        return terms;
    }

    /// <summary>
    /// Liefert die modellierten Bindewörter zwischen Auswahloptionen in
    /// <paramref name="isoCode"/>. Ohne Eintrag bleibt die Liste leer.
    /// </summary>
    public static IReadOnlyList<string> GetChoiceConnectives(string isoCode)
    {
        if (string.IsNullOrWhiteSpace(isoCode))
        {
            return [];
        }

        return Index().TryGetChoiceConnectives(isoCode.Trim(), out var connectives) ? [.. connectives] : [];
    }

    private static FormTranscriptLexiconIndex Index()
        => new(EmbeddedFormTranscriptLexiconProvider.Instance.GetLexicon());

    private static bool IsNonAsciiLetterOrSymbol(string value)
        => value.Length == 1
            && (value[0] is < '0' or > '9')
            && (value[0] is < 'a' or > 'z');

    private static void AddDistinct(
        ICollection<string> terms,
        ISet<string> seen,
        IEnumerable<string> candidates)
    {
        foreach (var candidate in candidates)
        {
            if (!string.IsNullOrWhiteSpace(candidate) && seen.Add(candidate))
            {
                terms.Add(candidate);
            }
        }
    }
}
