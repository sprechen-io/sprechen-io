namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Bereitet Lexikondaten für sprachbezogene Vergleiche und Buchstabierausgabe auf
/// und bewahrt Originalschreibweisen gesondert für Erkennungshinweise.
/// </summary>
internal sealed class FormTranscriptLexiconIndex
{
    internal FormTranscriptLexiconIndex(FormTranscriptLexicon lexicon)
    {
        ArgumentNullException.ThrowIfNull(lexicon);

        SpellingTokenMap = FormTranscriptLexiconNormalization.NormalizeKeys(lexicon.SpellingTokens);
        SpellingTokenSpellings = [.. lexicon.SpellingTokens.Keys];
        SpellingOutput = new FormSpellingOutputLexiconIndex(
            lexicon.SpellingAlphabets,
            lexicon.SpokenSpellingFormatByLanguage,
            SpellingTokenMap);
        SpellingConnectorsByLanguage = lexicon.SpellingConnectorsByLanguage.ToDictionary(
            entry => entry.Key,
            entry => (IReadOnlySet<string>)FormTranscriptLexiconNormalization.NormalizeWordTokens(entry.Value),
            StringComparer.OrdinalIgnoreCase);
        DiacriticsConnectorTokensByLanguage = lexicon.DiacriticsConnectorTokensByLanguage.ToDictionary(
            entry => entry.Key,
            entry => (IReadOnlySet<string>)FormTranscriptLexiconNormalization.NormalizeWordTokens(entry.Value),
            StringComparer.OrdinalIgnoreCase);
        UmlautDescriptorTokens = new HashSet<string>(lexicon.UmlautDescriptors, StringComparer.Ordinal);
        AcuteDescriptorTokens = new HashSet<string>(lexicon.AcuteDescriptors, StringComparer.Ordinal);
        UmlautByBaseToken = new Dictionary<string, string>(lexicon.UmlautByBaseToken, StringComparer.Ordinal);
        AcuteByBaseToken = new Dictionary<string, string>(lexicon.AcuteByBaseToken, StringComparer.Ordinal);
        NumberWordsByLanguage = lexicon.NumberWordsByLanguage.ToDictionary(
            entry => entry.Key,
            entry => FormTranscriptLexiconNormalization.NormalizeNumberWords(entry.Value),
            StringComparer.OrdinalIgnoreCase);
        NumberWordSpellingsByLanguage = lexicon.NumberWordsByLanguage.ToDictionary(
            entry => entry.Key,
            entry => (IReadOnlyList<string>)[
                .. entry.Value.Units.Keys,
                .. entry.Value.Teens.Keys,
                .. entry.Value.Tens.Keys,
                .. entry.Value.Scales.Keys],
            StringComparer.OrdinalIgnoreCase);
        ChoiceConnectivesByLanguage = lexicon.ChoiceConnectivesByLanguage.ToDictionary(
            entry => entry.Key,
            entry => (IReadOnlySet<string>)FormTranscriptLexiconNormalization.NormalizeWordTokens(entry.Value),
            StringComparer.OrdinalIgnoreCase);
        FillerTokensByLanguage = lexicon.FillerTokensByLanguage.ToDictionary(
            entry => entry.Key,
            entry => (IReadOnlySet<string>)FormTranscriptLexiconNormalization.NormalizeWordTokens(entry.Value),
            StringComparer.OrdinalIgnoreCase);
        CommandPolitePaddingTokensByLanguage = lexicon.CommandPolitePaddingTokensByLanguage.ToDictionary(
            entry => entry.Key,
            entry => (IReadOnlySet<string>)FormTranscriptLexiconNormalization.NormalizeWordTokens(entry.Value),
            StringComparer.OrdinalIgnoreCase);
        UppercaseMarkersByLanguage = lexicon.UppercaseMarkersByLanguage.ToDictionary(
            entry => entry.Key,
            entry => (IReadOnlySet<string>)FormTranscriptLexiconNormalization.NormalizeWordTokens(entry.Value),
            StringComparer.OrdinalIgnoreCase);
        UppercaseMarkerPlacement = new FormUppercaseMarkerPlacementLexicon(
            new Dictionary<string, string>(
                lexicon.UppercaseMarkerPlacementByLanguage,
                StringComparer.OrdinalIgnoreCase));
        DateInputVocabularyByLanguage = lexicon.DateInputVocabularyByLanguage.ToDictionary(
            entry => entry.Key,
            entry => FormTranscriptLexiconNormalization.NormalizeDateInputVocabulary(entry.Value),
            StringComparer.OrdinalIgnoreCase);
        TimeInputKeywordsByLanguage = new Dictionary<string, FormTimeInputKeywords>(
            lexicon.TimeInputKeywordsByLanguage,
            StringComparer.OrdinalIgnoreCase);
        NumberGrammarByLanguage = new Dictionary<string, FormNumberGrammarLexicon>(
            lexicon.NumberGrammarByLanguage,
            StringComparer.OrdinalIgnoreCase);
        DateInputParticlesByLanguage = lexicon.DateInputParticlesByLanguage.ToDictionary(
            entry => entry.Key,
            entry => (IReadOnlySet<string>)FormTranscriptLexiconNormalization.NormalizeWordTokens(entry.Value),
            StringComparer.OrdinalIgnoreCase);
        OrdinalDaysByLanguage = new Dictionary<string, FormOrdinalDayVocabulary>(
            lexicon.OrdinalDaysByLanguage,
            StringComparer.OrdinalIgnoreCase);
    }

    internal IReadOnlyDictionary<string, string> SpellingTokenMap { get; }

    private FormSpellingOutputLexiconIndex SpellingOutput { get; }

    internal IReadOnlyDictionary<string, IReadOnlyDictionary<char, string>> SpellingAlphabets
        => SpellingOutput.Alphabets;

    internal IReadOnlyDictionary<string, IReadOnlySet<string>> SpellingConnectorsByLanguage { get; }

    internal IReadOnlyDictionary<string, IReadOnlySet<string>> DiacriticsConnectorTokensByLanguage { get; }

    internal IReadOnlySet<string> UmlautDescriptorTokens { get; }

    internal IReadOnlySet<string> AcuteDescriptorTokens { get; }

    internal IReadOnlyDictionary<string, string> UmlautByBaseToken { get; }

    internal IReadOnlyDictionary<string, string> AcuteByBaseToken { get; }

    // Zahlworttabellen je Sprache. Die Zusammensetzung bleibt bei den Sprachparsern.
    internal IReadOnlyDictionary<string, FormNumberWordLexicon> NumberWordsByLanguage { get; }

    // Normalisierte Bindewörter zwischen Auswahloptionen je Sprache.
    // Kurze Wörter wie „e“ und „y“ sollen nur in ihrer Sprache trennen.
    internal IReadOnlyDictionary<string, IReadOnlySet<string>> ChoiceConnectivesByLanguage { get; }

    // Normalisierte Füllwörter je Sprache für Auswahl-, Buchstabier- und Ziffernparser.
    internal IReadOnlyDictionary<string, IReadOnlySet<string>> FillerTokensByLanguage { get; }

    // Normalisierte Höflichkeitswörter je Sprache. Für Befehle wird unten
    // eine sprachübergreifende Menge gebildet.
    internal IReadOnlyDictionary<string, IReadOnlySet<string>> CommandPolitePaddingTokensByLanguage { get; }

    // Normalisierte Großschreibungsmarker je Sprache für das Buchstabieren.
    internal IReadOnlyDictionary<string, IReadOnlySet<string>> UppercaseMarkersByLanguage { get; }

    internal FormUppercaseMarkerPlacementLexicon UppercaseMarkerPlacement { get; }

    /// <summary>
    /// Bewahrt die ursprünglichen Zahlwortschreibweisen je Sprache für Erkennungshinweise.
    /// Parser verwenden die normalisierten Vergleichsschlüssel. Der Erkenner benötigt
    /// dagegen die modellierten Schreibweisen einschließlich Diakritika.
    /// </summary>
    internal IReadOnlyDictionary<string, IReadOnlyList<string>> NumberWordSpellingsByLanguage { get; }

    /// <summary>
    /// Bewahrt die ursprünglichen Buchstabiertokens des Modells neben den Vergleichsschlüsseln.
    /// </summary>
    internal IReadOnlyList<string> SpellingTokenSpellings { get; }

    // Wörter zur Datumseingabe je Sprache mit abgeleiteten Vergleichsschlüsseln
    // für relative Tagesangaben und Ordinalstämme.
    internal IReadOnlyDictionary<string, FormDateInputVocabulary> DateInputVocabularyByLanguage { get; }

    // Sprachbezogene Schlüsselwörter nach ihrer Rolle in der Uhrzeiteingabe.
    internal IReadOnlyDictionary<string, FormTimeInputKeywords> TimeInputKeywordsByLanguage { get; }

    // Sprachbezogene Grammatikdaten ergänzen die Zahlworttabellen.
    internal IReadOnlyDictionary<string, FormNumberGrammarLexicon> NumberGrammarByLanguage { get; }

    // Normalisierte Partikel je Sprache für die Datumsauswertung. Deutsche
    // Anfangspartikel liegen in DateInputVocabularyByLanguage.
    internal IReadOnlyDictionary<string, IReadOnlySet<string>> DateInputParticlesByLanguage { get; }

    // Ordinaltage je Sprache werden unverändert übernommen. Deutsche Stämme
    // und Endungen liegen in DateInputVocabularyByLanguage.
    internal IReadOnlyDictionary<string, FormOrdinalDayVocabulary> OrdinalDaysByLanguage { get; }

    // Deutsche Wörter und Wortformen für die Datumseingabe. Ohne Eintrag leer.
    internal FormDateInputVocabulary GermanDateInputVocabulary
        => DateInputVocabularyByLanguage.TryGetValue("de", out var germanDateInput)
            ? germanDateInput
            : new FormDateInputVocabulary();

    // Deutsche Schlüsselwörter für Uhrzeiteingaben. Ohne Eintrag leer.
    internal FormTimeInputKeywords GermanTimeInputKeywords
        => TimeInputKeywordsByLanguage.TryGetValue("de", out var germanTimeInput)
            ? germanTimeInput
            : new FormTimeInputKeywords();

    internal bool TryGetNumberGrammar(string? isoCode, out FormNumberGrammarLexicon grammar)
    {
        if (!string.IsNullOrWhiteSpace(isoCode)
            && NumberGrammarByLanguage.TryGetValue(isoCode.Trim(), out var match))
        {
            grammar = match;
            return true;
        }

        grammar = new FormNumberGrammarLexicon();
        return false;
    }

    internal bool TryGetDateInputParticles(string? isoCode, out IReadOnlySet<string> particles)
    {
        if (!string.IsNullOrWhiteSpace(isoCode)
            && DateInputParticlesByLanguage.TryGetValue(isoCode.Trim(), out var match))
        {
            particles = match;
            return true;
        }

        particles = EmptyTokens;
        return false;
    }

    internal bool TryGetOrdinalDays(string? isoCode, out FormOrdinalDayVocabulary ordinalDays)
    {
        if (!string.IsNullOrWhiteSpace(isoCode)
            && OrdinalDaysByLanguage.TryGetValue(isoCode.Trim(), out var match))
        {
            ordinalDays = match;
            return true;
        }

        ordinalDays = new FormOrdinalDayVocabulary();
        return false;
    }

    internal bool TryGetSpokenSpellingFormat(string? isoCode, out FormSpokenSpellingFormatWords formatWords)
        => SpellingOutput.TryGetFormat(isoCode, out formatWords);

    internal bool TryGetSpellingAlphabet(string alphabetName, out IReadOnlyDictionary<char, string> alphabet)
        => SpellingOutput.TryGetAlphabet(alphabetName, out alphabet);

    // Vorzeichenwörter aller Sprachen ohne Duplikate in Lexikonreihenfolge liefern.
    // Die Vorzeichenprüfung findet vor der Wahl der Sprachstrategie statt.
    internal IReadOnlyList<string> GetSignWords()
    {
        var signWords = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var grammar in NumberGrammarByLanguage.Values)
        {
            foreach (var word in grammar.SignWords)
            {
                if (seen.Add(word))
                {
                    signWords.Add(word);
                }
            }
        }

        return signWords;
    }

    // Höflichkeitswörter aller modellierten Sprachen gemeinsam liefern.
    // Die Befehlsauswertung verwendet diese vereinigte Menge unabhängig von der Sitzungssprache.
    internal IReadOnlySet<string> GetCommandPolitePaddingTokens()
    {
        var union = new HashSet<string>(StringComparer.Ordinal);
        foreach (var tokens in CommandPolitePaddingTokensByLanguage.Values)
        {
            union.UnionWith(tokens);
        }

        return union;
    }

    // Deutsche Zahlworttabelle für Auswahlindex- und Zeitparser. Ohne Eintrag leer.
    internal FormNumberWordLexicon GermanNumberWords
        => NumberWordsByLanguage.TryGetValue("de", out var germanNumberWords)
            ? germanNumberWords
            : new FormNumberWordLexicon();

    internal bool TryGetNumberWords(string? isoCode, out FormNumberWordLexicon numberWords)
    {
        if (!string.IsNullOrWhiteSpace(isoCode)
            && NumberWordsByLanguage.TryGetValue(isoCode.Trim(), out var match))
        {
            numberWords = match;
            return true;
        }

        numberWords = new FormNumberWordLexicon();
        return false;
    }

    internal bool TryGetChoiceConnectives(string? isoCode, out IReadOnlySet<string> connectives)
    {
        if (!string.IsNullOrWhiteSpace(isoCode)
            && ChoiceConnectivesByLanguage.TryGetValue(isoCode.Trim(), out var match))
        {
            connectives = match;
            return true;
        }

        connectives = EmptyTokens;
        return false;
    }

    // Füllwörter der angefragten Sprache verwenden, ersatzweise deutsche Wörter.
    // Die Mengen werden nicht sprachübergreifend vereinigt.
    internal IReadOnlySet<string> GetFillerTokens(string? isoCode)
        => GetPerLanguageTokens(FillerTokensByLanguage, isoCode);

    // Bindewörter beim Buchstabieren in der angefragten Sprache verwenden, ersatzweise deutsche.
    // So werden kurze romanische Bindewörter nicht in jeder Sprache übersprungen.
    internal IReadOnlySet<string> GetSpellingConnectors(string? isoCode)
        => GetPerLanguageTokens(SpellingConnectorsByLanguage, isoCode);

    // Bindewörter vor Diakritika in der angefragten Sprache, ersatzweise deutsche Wörter.
    internal IReadOnlySet<string> GetDiacriticsConnectorTokens(string? isoCode)
        => GetPerLanguageTokens(DiacriticsConnectorTokensByLanguage, isoCode);

    // Großschreibungsmarker der angefragten Sprache, ersatzweise deutsche Wörter.
    internal IReadOnlySet<string> GetUppercaseMarkers(string? isoCode)
        => GetPerLanguageTokens(UppercaseMarkersByLanguage, isoCode);

    private IReadOnlySet<string> GetPerLanguageTokens(
        IReadOnlyDictionary<string, IReadOnlySet<string>> tokensByLanguage,
        string? isoCode)
    {
        if (!string.IsNullOrWhiteSpace(isoCode)
            && tokensByLanguage.TryGetValue(isoCode.Trim(), out var match))
        {
            return match;
        }

        return tokensByLanguage.TryGetValue("de", out var german)
            ? german
            : EmptyTokens;
    }

    private static readonly IReadOnlySet<string> EmptyTokens =
        new HashSet<string>(StringComparer.Ordinal);
}
