using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.Speech.ModelRuntime.Services;

/// <summary>
/// Klassifiziert Zustimmung und Ablehnung anhand des modellierten Sprachlexikons.
/// Nichtverstehensausdrücke haben Vorrang. Mehrteilige Ja-Nein-Ausdrücke belegen
/// ihre Wörter vor der Einzelwortzählung. Zustimmung zusammen mit Ablehnung oder
/// einem übrigen Negationshinweis ergibt <see cref="YesNoClassification.Ambiguous"/>.
/// Ein Negationshinweis allein ergibt <see cref="YesNoClassification.NotRecognized"/>.
/// </summary>
/// <remarks>
/// Negationshinweise werden an jeder noch unmaskierten Stelle berücksichtigt.
/// Damit können auch zustimmende Wendungen wie "Ja, kein Problem" mehrdeutig werden.
/// Modellierte Nichtverstehensausdrücke ergeben vor jeder Ja-Nein-Zählung
/// <see cref="YesNoClassification.NotRecognized"/>, damit fehlendes Verständnis
/// nicht als Zustimmung oder Ablehnung ausgelegt wird.
/// </remarks>
public sealed class YesNoTranscriptClassifier : IYesNoTranscriptClassifier
{
    public YesNoClassification Classify(string? requestedIsoCode, string transcript)
    {
        var yesTokens = NormalizeTokens(VoiceInterfaceModelCatalog.Lexicon.YesNo.GetYesTokens(requestedIsoCode!));
        var noTokens = NormalizeTokens(VoiceInterfaceModelCatalog.Lexicon.YesNo.GetNoTokens(requestedIsoCode!));
        var negationCues = NormalizeTokens(VoiceInterfaceModelCatalog.Lexicon.YesNo.GetNegationCues(requestedIsoCode!));
        var notUnderstood = NormalizeTokens(
            VoiceInterfaceModelCatalog.Lexicon.YesNo.GetNotUnderstoodPhrases(requestedIsoCode!));
        return Classify(transcript, yesTokens, noTokens, negationCues, notUnderstood);
    }

    public YesNoClassification Classify(IEnumerable<string> requestedIsoCodes, string transcript)
    {
        ArgumentNullException.ThrowIfNull(requestedIsoCodes);

        var yesTokens = new HashSet<string>(StringComparer.Ordinal);
        var noTokens = new HashSet<string>(StringComparer.Ordinal);
        var negationCues = new HashSet<string>(StringComparer.Ordinal);
        var notUnderstood = new HashSet<string>(StringComparer.Ordinal);
        foreach (var isoCode in requestedIsoCodes)
        {
            yesTokens.UnionWith(NormalizeTokens(VoiceInterfaceModelCatalog.Lexicon.YesNo.GetYesTokens(isoCode)));
            noTokens.UnionWith(NormalizeTokens(VoiceInterfaceModelCatalog.Lexicon.YesNo.GetNoTokens(isoCode)));
            negationCues.UnionWith(NormalizeTokens(VoiceInterfaceModelCatalog.Lexicon.YesNo.GetNegationCues(isoCode)));
            notUnderstood.UnionWith(
                NormalizeTokens(VoiceInterfaceModelCatalog.Lexicon.YesNo.GetNotUnderstoodPhrases(isoCode)));
        }

        return Classify(transcript, yesTokens, noTokens, negationCues, notUnderstood);
    }

    private static YesNoClassification Classify(
        string transcript,
        HashSet<string> yesTokens,
        HashSet<string> noTokens,
        HashSet<string> negationCues,
        HashSet<string> notUnderstoodPhrases)
    {
        if (string.IsNullOrWhiteSpace(transcript) || (yesTokens.Count == 0 && noTokens.Count == 0))
        {
            return YesNoClassification.NotRecognized;
        }

        var normalized = TextNormalizer.NormalizeForPhraseMatching(transcript);
        var words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return YesNoClassification.NotRecognized;
        }

        var masked = new bool[words.Length];

        // Nichtverstehen vor jeder Ja-Nein-Zählung erkennen: Ein Wort aus einem Ausdruck
        // wie "non capisco" könnte sonst als fremdsprachiges Nein zählen.
        // Die mehrteilige Maskierung und die Einzelwortprüfung müssen deshalb
        // vor den Zustimmungs- und Ablehnungstreffern liegen.
        if (MaskMultiWordTokens(words, masked, notUnderstoodPhrases) > 0
            || ContainsSingleWordMarker(words, notUnderstoodPhrases))
        {
            return YesNoClassification.NotRecognized;
        }

        var noHits = MaskMultiWordTokens(words, masked, noTokens);
        var yesHits = MaskMultiWordTokens(words, masked, yesTokens);

        var singleYes = SingleWordTokens(yesTokens);
        var singleNo = SingleWordTokens(noTokens);
        var cueHits = 0;
        for (var index = 0; index < words.Length; index++)
        {
            if (masked[index])
            {
                continue;
            }

            if (singleYes.Contains(words[index]))
            {
                yesHits++;
            }

            if (singleNo.Contains(words[index]))
            {
                noHits++;
            }
            else if (negationCues.Contains(words[index]))
            {
                cueHits++;
            }
        }

        return Resolve(yesHits, noHits, cueHits);
    }

    private static YesNoClassification Resolve(int yesHits, int noHits, int cueHits)
    {
        // Ein bloßer Negationshinweis verhindert ein gleichzeitiges Ja, zählt selbst
        // aber nicht als Nein. Ohne Ja- oder Nein-Treffer bleibt die Antwort unerkannt.
        if (yesHits > 0 && (noHits > 0 || cueHits > 0))
        {
            return YesNoClassification.Ambiguous;
        }

        if (yesHits > 0)
        {
            return YesNoClassification.Yes;
        }

        if (noHits > 0)
        {
            return YesNoClassification.No;
        }

        return YesNoClassification.NotRecognized;
    }

    /// <summary>
    /// Prüft, ob ein einwortiger modellierter Hinweis auf Nichtverstehen im Transkript vorkommt.
    /// </summary>
    /// <remarks>
    /// <see cref="MaskMultiWordTokens"/> berücksichtigt nur Ausdrücke aus mindestens
    /// zwei Wörtern. Diese eigene Prüfung erfasst einwortige Hinweise, bevor sie
    /// bei der Ja-Nein-Auswertung als Zustimmung oder Ablehnung zählen könnten.
    /// </remarks>
    private static bool ContainsSingleWordMarker(string[] words, IEnumerable<string> phrases)
    {
        var singleWord = phrases
            .Select(phrase => phrase.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(phrase => phrase.Length == 1)
            .Select(phrase => phrase[0])
            .ToHashSet(StringComparer.Ordinal);

        return singleWord.Count > 0 && words.Any(singleWord.Contains);
    }

    private static int MaskMultiWordTokens(string[] words, bool[] masked, IEnumerable<string> tokens)
    {
        var phrases = tokens
            .Select(token => token.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(phrase => phrase.Length >= 2)
            .OrderByDescending(phrase => phrase.Length)
            .ThenBy(phrase => string.Join(' ', phrase), StringComparer.Ordinal)
            .ToArray();

        var hits = 0;
        foreach (var phrase in phrases)
        {
            var index = 0;
            while (index + phrase.Length <= words.Length)
            {
                if (PhraseMatchesAt(words, masked, phrase, index))
                {
                    for (var offset = 0; offset < phrase.Length; offset++)
                    {
                        masked[index + offset] = true;
                    }

                    hits++;
                    index += phrase.Length;
                }
                else
                {
                    index++;
                }
            }
        }

        return hits;
    }

    private static bool PhraseMatchesAt(string[] words, bool[] masked, string[] phrase, int start)
    {
        for (var offset = 0; offset < phrase.Length; offset++)
        {
            var position = start + offset;
            if (masked[position] || !string.Equals(words[position], phrase[offset], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static HashSet<string> NormalizeTokens(IEnumerable<string> tokens)
    {
        var normalized = new HashSet<string>(StringComparer.Ordinal);
        foreach (var token in tokens)
        {
            var normalizedToken = TextNormalizer.NormalizeForPhraseMatching(token);
            if (normalizedToken.Length != 0)
            {
                normalized.Add(normalizedToken);
            }
        }

        return normalized;
    }

    private static HashSet<string> SingleWordTokens(IEnumerable<string> tokens)
    {
        var single = new HashSet<string>(StringComparer.Ordinal);
        foreach (var token in tokens)
        {
            if (!token.Contains(' ', StringComparison.Ordinal))
            {
                single.Add(token);
            }
        }

        return single;
    }
}
