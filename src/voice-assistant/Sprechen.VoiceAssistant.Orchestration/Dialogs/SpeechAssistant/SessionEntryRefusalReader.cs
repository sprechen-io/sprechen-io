using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.LanguageContracts;
using Sprechen.Speech.TextNormalization;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft Ablehnungen vor der Annahme einer angebotenen Sprache. Verneinungswörter,
/// begrenztes Suchfenster und Sprachname bilden eine von der Annahmeprüfung getrennte Regel.
/// </summary>
internal static class SessionEntryRefusalReader
{
    private static readonly ILanguageCatalog Endonyms = VoiceInterfaceLanguageCatalog.CreateDefault();

    /// <summary>
    /// Prüft Nein-Wörter in Frage- und Sitzungssprache sowie Verneinungen in der Umgebung des
    /// angebotenen Eigennamens. Der begrenzte Namensbezug verhindert, dass jede fremdsprachige
    /// Verneinungsform irgendwo im Satz schon die Frage ablehnt.
    /// </summary>
    public static bool IsRefused(string transcript, string askedIsoCode, string sessionIsoCode)
    {
        var normalized = TextNormalizer.NormalizeForPhraseMatching(transcript);

        foreach (var isoCode in LanguagesTheAnswerCanBeIn(askedIsoCode, sessionIsoCode))
        {
            foreach (var word in VoiceInterfaceModelCatalog.Lexicon.YesNo.GetNoTokens(isoCode))
            {
                if (TextNormalizer.MatchesAsWordOrPhrase(
                        normalized,
                        TextNormalizer.NormalizeForPhraseMatching(word)))
                {
                    return true;
                }
            }
        }

        return NegatesTheOfferedName(normalized, askedIsoCode, sessionIsoCode);
    }

    /// <summary>
    /// Prüft Verneinungen im begrenzten Vorlauf des angebotenen Eigennamens und über eine eigene Regel
    /// hinter dem Namen. So genügt nicht allein das Vorkommen einer Verneinungsform an beliebiger
    /// Stelle.
    /// </summary>
    private static bool NegatesTheOfferedName(
        string normalizedTranscript,
        string askedIsoCode,
        string sessionIsoCode)
    {
        if (!LanguageCode.TryParseIso6391(askedIsoCode, out var language))
        {
            return false;
        }

        var endonym = TextNormalizer.NormalizeForPhraseMatching(Endonyms.GetEndonym(language));
        if (endonym.Length == 0)
        {
            return false;
        }

        var words = normalizedTranscript.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Auch einen Namen am Satzanfang prüfen: Eine zugehörige Verneinung kann dahinter stehen, etwa
        // in „Deutsch verstehe ich nicht.“.
        for (var i = 0; i < words.Length; i++)
        {
            if (!string.Equals(words[i], endonym, StringComparison.Ordinal))
            {
                continue;
            }

            // Das Fenster vor dem Namen begrenzen: Bloße Nachbarschaft verfehlt „non voglio italiano“,
            // unbegrenzter Vorlauf kann dagegen die Ablehnung einer anderen Sprache zuordnen. Für den
            // Nachlauf gilt die gesonderte Regel.
            var from = Math.Max(0, i - (MaxWordsBetweenANegationAndTheName + 1));
            var runUpToTheName = string.Join(' ', words[from..i]);

            // Verneinungen als Wortfolgen vergleichen, weil die Normalisierung beispielsweise „don’t“
            // in mehrere Wörter zerlegen kann. Ein Vergleich nur einzelner Wörter würde solche
            // modellierten Formen übersehen.
            foreach (var negation in EveryNegation)
            {
                if (TextNormalizer.MatchesAsWordOrPhrase(runUpToTheName, negation))
                {
                    return true;
                }
            }

            if (NegationAfterTheNameBelongsToIt(words, i, normalizedTranscript, askedIsoCode, sessionIsoCode))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Prüft Verneinung im Nachlauf des angebotenen Namens. Ein Ja-Wort der Frage- oder Sitzungssprache
    /// schließt diese Nachlaufregel aus. Ein dort genannter anderer Sprachname verhindert ebenfalls die
    /// Zuordnung zur angebotenen Sprache.
    /// </summary>
    private static bool NegationAfterTheNameBelongsToIt(
        string[] words,
        int nameIndex,
        string normalizedTranscript,
        string askedIsoCode,
        string sessionIsoCode)
    {
        // Ein Ja-Wort zusammen mit dem bereits gefundenen Eigennamen nimmt diese Nachlaufregel aus.
        // Dadurch wird eine nachgestellte Wendung wie „non c’è problema“ nicht allein zur Ablehnung
        // der angebotenen Sprache. Die übrigen Ablehnungsprüfungen bleiben bestehen.
        if (SaysAnyYesWord(normalizedTranscript, askedIsoCode, sessionIsoCode))
        {
            return false;
        }

        for (var j = nameIndex + 1; j < words.Length; j++)
        {
            var tail = string.Join(' ', words[j..]);
            var negated = false;
            foreach (var negation in EveryNegation)
            {
                if (TextNormalizer.MatchesAsWordOrPhrase(tail, negation))
                {
                    negated = true;
                    break;
                }
            }

            if (!negated)
            {
                continue;
            }

            return !AnotherSupportedLanguageIsNamedIn(tail, askedIsoCode, sessionIsoCode);
        }

        return false;
    }

    /// <summary>
    /// Prüft andere unterstützte Sprachnamen im gegebenen Text anhand ihres Eigennamens und ihrer
    /// Bezeichnungen in Frage- und Sitzungssprache, damit etwa eine Verneinung von „inglese“ als Bezug
    /// auf Englisch erkennbar bleibt.
    /// </summary>
    private static bool AnotherSupportedLanguageIsNamedIn(
        string normalizedText,
        string askedIsoCode,
        string sessionIsoCode)
    {
        foreach (var isoCode in VoiceInterfaceModelCatalog.SupportedLanguages)
        {
            if (string.Equals(isoCode, askedIsoCode, StringComparison.OrdinalIgnoreCase)
                || !LanguageCode.TryParseIso6391(isoCode, out var language))
            {
                continue;
            }

            foreach (var name in NamesFor(language, askedIsoCode, sessionIsoCode))
            {
                if (name.Length != 0 && TextNormalizer.MatchesAsWordOrPhrase(normalizedText, name))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Prüft ein Ja-Wort der Frage- oder Sitzungssprache für die Ausnahme der Nachlaufregel. Dies ist
    /// keine selbstständige Annahmeregel. Gemeinsam modellierte Ja-Formen können hier zählen, ohne eine
    /// Sprache allein anzunehmen.
    /// </summary>
    private static bool SaysAnyYesWord(
        string normalizedTranscript,
        string askedIsoCode,
        string sessionIsoCode)
    {
        foreach (var isoCode in LanguagesTheAnswerCanBeIn(askedIsoCode, sessionIsoCode))
        {
            foreach (var token in VoiceInterfaceModelCatalog.Lexicon.YesNo.GetYesTokens(isoCode))
            {
                var normalizedToken = TextNormalizer.NormalizeForPhraseMatching(token);
                if (normalizedToken.Length != 0
                    && TextNormalizer.MatchesAsWordOrPhrase(normalizedTranscript, normalizedToken))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Liefert für die Vergleichssprache ihren Eigennamen sowie ihre Bezeichnungen in Frage- und
    /// Sitzungssprache.
    /// </summary>
    private static IEnumerable<string> NamesFor(
        LanguageCode language,
        string askedIsoCode,
        string sessionIsoCode)
    {
        yield return TextNormalizer.NormalizeForPhraseMatching(Endonyms.GetEndonym(language));

        foreach (var speakerIsoCode in new[] { askedIsoCode, sessionIsoCode })
        {
            if (!LanguageCode.TryParseIso6391(speakerIsoCode, out var speaker))
            {
                continue;
            }

            yield return TextNormalizer.NormalizeForPhraseMatching(
                Endonyms.GetDisplayName(language, speaker));
        }
    }

    /// <summary>
    /// Beschränkt die Suche vor dem Namen auf die letzten drei Wörter. Bei einer
    /// einwortigen Verneinung können damit höchstens zwei Wörter zwischen ihr und dem Namen
    /// liegen. Mehrwortige Verneinungen müssen vollständig in dieses Fenster passen.
    /// </summary>
    private const int MaxWordsBetweenANegationAndTheName = 2;

    /// <summary>
    /// Vereinigt die modellierten Nein-Wörter und Verneinungshinweise aller unterstützten Sprachen für
    /// die Prüfung im Namensumfeld.
    /// </summary>
    private static readonly IReadOnlyList<string> EveryNegation = BuildNegations();

    private static IReadOnlyList<string> BuildNegations()
    {
        var negations = new HashSet<string>(StringComparer.Ordinal);
        foreach (var isoCode in VoiceInterfaceModelCatalog.SupportedLanguages)
        {
            foreach (var word in VoiceInterfaceModelCatalog.Lexicon.YesNo.GetNoTokens(isoCode)
                         .Concat(VoiceInterfaceModelCatalog.Lexicon.YesNo.GetNegationCues(isoCode)))
            {
                var normalized = TextNormalizer.NormalizeForPhraseMatching(word);
                if (normalized.Length > 0)
                {
                    negations.Add(normalized);
                }
            }
        }

        return [.. negations];
    }
    private static IEnumerable<string> LanguagesTheAnswerCanBeIn(string askedIsoCode, string sessionIsoCode)
        => string.Equals(askedIsoCode, sessionIsoCode, StringComparison.OrdinalIgnoreCase)
            ? [askedIsoCode]
            : [askedIsoCode, sessionIsoCode];
}
