using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.LanguageContracts;
using Sprechen.Speech.ModelRuntime.Services;
using Sprechen.Speech.TextNormalization;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft die Annahme einer angebotenen Sprache unabhängig vom Ablauf der Einstiegsfragen. Ablehnung hat
/// Vorrang. Danach entscheiden der Eigenname der angebotenen Sprache oder ein zulässiges Ja-Wort dieser
/// Sprache. Sprachübergreifend kollidierende Ja-Wörter genügen nicht, damit ein gemeinsamer Ausdruck
/// keine beliebige frühere Frage annimmt.
/// </summary>
internal static class SessionEntryAnswerReader
{
    private static readonly ILanguageCatalog Endonyms = VoiceInterfaceLanguageCatalog.CreateDefault();

    /// <summary>
    /// Einmal aus dem Modell gebildete Bestandteile der Aktivierungsformen, die nicht als
    /// eigenständiges Ja zählen sollen.
    /// </summary>
    private static readonly HashSet<string> WordsTheParticipantWasToldToSay = BuildActivationWords();

    /// <summary>
    /// Prüft die Annahme der Sitzungssprache mit derselben Regel wie spätere Sprachangebote, damit die
    /// erste Frage keine weiter gefasste Zustimmung zulässt.
    /// </summary>
    public static bool AcceptsTheSessionLanguage(string? transcript, string askedIsoCode)
        => Accepts(transcript, askedIsoCode, sessionIsoCode: askedIsoCode);

    /// <summary>
    /// Prüft die Annahme der angebotenen Sprache unter Einbezug der Sitzungssprache bei der
    /// Ablehnungsprüfung. Die Annahme selbst benötigt den angebotenen Eigennamen oder ein zulässiges
    /// eigenes Ja.
    /// </summary>
    public static bool AcceptsTheOfferedLanguage(string? transcript, string askedIsoCode, string sessionIsoCode)
        => Accepts(transcript, askedIsoCode, sessionIsoCode);

    /// <summary>
    /// Prüft Ablehnung vor dem Eigennamen und Ja-Wörter zuletzt. Dadurch reicht ein Sprachname in einer
    /// erkannten Verneinung nicht zur Annahme. Ein fremder Eigenname verhindert die Ja-Annahme, sofern
    /// der angebotene Eigenname nicht bereits entschieden hat.
    /// </summary>
    private static bool Accepts(string? transcript, string askedIsoCode, string sessionIsoCode)
    {
        if (string.IsNullOrWhiteSpace(transcript))
        {
            return false;
        }

        if (SessionEntryRefusalReader.IsRefused(transcript, askedIsoCode, sessionIsoCode))
        {
            return false;
        }

        if (IsTheOfferedLanguagesOwnName(transcript, askedIsoCode))
        {
            return true;
        }

        if (NamesADifferentSupportedLanguage(transcript, askedIsoCode))
        {
            return false;
        }

        // Nichtverstehen nach dem angebotenen Eigennamen, aber vor einem bloßen Ja prüfen. Ein Satz
        // kann eine Höflichkeits- oder Rückfrageform tragen und zugleich die angebotene Sprache
        // ausdrücklich benennen.
        if (IsNonComprehension(transcript))
        {
            return false;
        }

        return SaysYesInTheAskedLanguage(transcript, askedIsoCode);
    }

    /// <summary>
    /// Prüft ein eigenes Ja-Wort mit erhaltenen diakritischen Zeichen und schließt
    /// Aktivierungsbestandteile sowie sprachübergreifend kollidierende Formen aus. Die Kollisionsmenge
    /// wird ohne diakritische Zeichen gebildet. Für gemeinsam modellierte Formen wie sí und sì bleibt der
    /// angebotene Eigenname als Antwortweg.
    /// </summary>
    private static bool SaysYesInTheAskedLanguage(string transcript, string askedIsoCode)
    {
        var accented = TextNormalizer.NormalizeForAccentSensitiveMatching(transcript);

        foreach (var token in VoiceInterfaceModelCatalog.Lexicon.YesNo.GetYesTokens(askedIsoCode))
        {
            var accentedToken = TextNormalizer.NormalizeForAccentSensitiveMatching(token);
            if (accentedToken.Length == 0
                || !TextNormalizer.MatchesAsWordOrPhrase(accented, accentedToken))
            {
                continue;
            }

            if (WordsTheParticipantWasToldToSay.Contains(
                    TextNormalizer.NormalizeForPhraseMatching(accentedToken)))
            {
                continue;
            }

            if (YesWordsMoreThanOneLanguageSaysTheSameWay.Contains(
                    TextNormalizer.NormalizeForPhraseMatching(accentedToken)))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Hält Ja-Wörter, deren Form ohne diakritische Zeichen in mehreren Modellsprachen vorkommt. Die
    /// Ableitung aus dem Katalog berücksichtigt Änderungen des Wortbestands, ohne eine zweite
    /// Kollisionsliste zu pflegen.
    /// </summary>
    private static readonly HashSet<string> YesWordsMoreThanOneLanguageSaysTheSameWay
        = BuildYesWordsMoreThanOneLanguageSaysTheSameWay();

    private static HashSet<string> BuildYesWordsMoreThanOneLanguageSaysTheSameWay()
    {
        var owners = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var isoCode in VoiceInterfaceModelCatalog.SupportedLanguages)
        {
            foreach (var token in VoiceInterfaceModelCatalog.Lexicon.YesNo.GetYesTokens(isoCode))
            {
                var folded = TextNormalizer.NormalizeForPhraseMatching(token);
                if (folded.Length == 0)
                {
                    continue;
                }

                if (!owners.TryGetValue(folded, out var languages))
                {
                    languages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    owners[folded] = languages;
                }

                languages.Add(isoCode);
            }
        }

        return [.. owners.Where(entry => entry.Value.Count > 1).Select(entry => entry.Key)];
    }

    /// <summary>
    /// Prüft auf den Eigennamen einer anderen unterstützten Sprache, bevor ein bloßes Ja die angebotene
    /// Sprache annehmen kann.
    /// </summary>
    private static bool NamesADifferentSupportedLanguage(string transcript, string askedIsoCode)
    {
        foreach (var isoCode in VoiceInterfaceModelCatalog.SupportedLanguages)
        {
            if (string.Equals(isoCode, askedIsoCode, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (IsTheOfferedLanguagesOwnName(transcript, isoCode))
            {
                return true;
            }
        }

        return false;
    }

    private static HashSet<string> BuildActivationWords()
    {
        var words = new HashSet<string>(StringComparer.Ordinal);
        foreach (var form in VoiceInterfaceModelCatalog.WakeWord.GetAllActivationForms()
                     .Concat(VoiceInterfaceModelCatalog.SupportedLanguages
                         .SelectMany(VoiceInterfaceModelCatalog.WakeWord.GetRecognitionOnlyActivationFormsForLanguage)))
        {
            foreach (var word in TextNormalizer.NormalizeForPhraseMatching(form)
                         .Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                words.Add(word);
            }
        }

        return words;
    }


    /// <summary>
    /// Prüft den Eigennamen der angebotenen Sprache als Wort oder Wortfolge im Transkript. Die Frage
    /// legt die Zielsprache bereits fest. Deshalb gilt hier eine engere Entscheidungsaufgabe als beim
    /// allgemeinen Sprachzielparser. Fremdbezeichnungen werden nicht ergänzt, und weitere Wörter
    /// schließen den Treffer nicht pauschal aus. Die Ablehnungsprüfung liegt beim Aufrufer davor.
    /// </summary>
    private static bool IsTheOfferedLanguagesOwnName(string transcript, string askedIsoCode)
    {
        if (!LanguageCode.TryParseIso6391(askedIsoCode, out var language))
        {
            return false;
        }

        var normalized = TextNormalizer.NormalizeForPhraseMatching(transcript);
        var endonym = Endonyms.GetEndonym(language);
        return !string.IsNullOrWhiteSpace(endonym)
            && TextNormalizer.MatchesAsWordOrPhrase(
                normalized,
                TextNormalizer.NormalizeForPhraseMatching(endonym));
    }

    /// <summary>
    /// Prüft modellierte Ausdrücke des Nichtverstehens über alle unterstützten Sprachen. Im
    /// Annahmeablauf verhindern sie ein bloßes Ja, nachdem Ablehnung und Sprachennamen bereits geprüft
    /// wurden.
    /// </summary>
    public static bool IsNonComprehension(string? transcript)
    {
        if (string.IsNullOrWhiteSpace(transcript))
        {
            return false;
        }

        var normalized = TextNormalizer.NormalizeForPhraseMatching(transcript);
        foreach (var isoCode in VoiceInterfaceModelCatalog.SupportedLanguages)
        {
            foreach (var phrase in VoiceInterfaceModelCatalog.Lexicon.YesNo.GetNotUnderstoodPhrases(isoCode))
            {
                if (TextNormalizer.MatchesAsWordOrPhrase(
                        normalized,
                        TextNormalizer.NormalizeForPhraseMatching(phrase)))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
