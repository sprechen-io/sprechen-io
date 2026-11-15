using System.Collections.Immutable;
using System.Globalization;

namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet normalisierte Auswahläußerungen den vorhandenen Optionen zu
/// und trennt eindeutige Treffer, Mehrdeutigkeit und fehlende Zuordnung.
/// </summary>
internal sealed class FormChoiceOptionMatcher
{
    private static readonly string[] OrdinalDeclensionEndings = ["e", "er", "es", "en", "em"];

    // Deutsche Ordinalstämme für 1 bis 12 werden mit den Endungen zu Wörtern ergänzt.
    // Allein oder nach einem Schlüsselwort bezeichnen sie einen einsbasierten Optionsindex.
    private static readonly (string Stem, int Value)[] OrdinalStems =
    [
        ("erst", 1), ("zweit", 2), ("dritt", 3), ("viert", 4), ("funft", 5), ("sechst", 6),
        ("siebt", 7), ("siebent", 7), ("acht", 8), ("neunt", 9), ("zehnt", 10), ("elft", 11), ("zwolft", 12)
    ];

    private static readonly IReadOnlyDictionary<string, int> OrdinalWords = BuildOrdinalWords();

    private static readonly IReadOnlySet<string> OrdinalKeywords =
        new HashSet<string>(["option", "nummer", "die", "der", "das"], StringComparer.Ordinal);

    private readonly FormTranscriptLexiconIndex _lexicon;
    private readonly FormSpokenNumberWordParser _numberWordParser;

    public FormChoiceOptionMatcher(FormTranscriptLexiconIndex lexicon)
    {
        ArgumentNullException.ThrowIfNull(lexicon);

        _lexicon = lexicon;
        lexicon.TryGetNumberGrammar("de", out var germanNumberGrammar);
        _numberWordParser = new FormSpokenNumberWordParser(lexicon.GermanNumberWords, germanNumberGrammar);
    }

    /// <summary>
    /// Prüft zuerst exakte Bezeichnung oder Code, auch nach dem Entfernen von Füllwörtern, dann
    /// einen Optionsindex. Erst danach sperren Negationswörter die Suche nach enthaltenen
    /// Bezeichnungen. Mehrere solche Treffer werden als mehrdeutig zurückgegeben.
    /// </summary>
    public (string? Code, ImmutableArray<string> AmbiguousCandidates) ResolveSingle(
        string? requestedIsoCode,
        string normalizedCandidate,
        ImmutableArray<FormChoiceOption> options,
        IReadOnlySet<string> vetoTokens)
    {
        if (TryMatchExact(normalizedCandidate, options, out var exactCode))
        {
            return (exactCode, ImmutableArray<string>.Empty);
        }

        var candidate = StripFillers(requestedIsoCode, normalizedCandidate);
        if (candidate.Length == 0)
        {
            return (null, ImmutableArray<string>.Empty);
        }

        if (TryMatchExact(candidate, options, out var strippedExactCode))
        {
            return (strippedExactCode, ImmutableArray<string>.Empty);
        }

        if (TryParseOrdinalPhrase(candidate, out var index))
        {
            return index >= 1 && index <= options.Length
                ? (options[index - 1].Code, ImmutableArray<string>.Empty)
                : (null, ImmutableArray<string>.Empty);
        }

        if (ContainsNegation(candidate, vetoTokens))
        {
            return (null, ImmutableArray<string>.Empty);
        }

        var hits = ContainmentHits(candidate, options, vetoTokens);
        return hits.Length switch
        {
            1 => (hits[0], ImmutableArray<string>.Empty),
            >= 2 => (null, hits),
            _ => (null, ImmutableArray<string>.Empty)
        };
    }

    /// <summary>
    /// Prüft die gesamte Mehrfachauswahläußerung zuerst auf einen exakten Treffer oder Optionsindex.
    /// </summary>
    public bool TryMatchWhole(string candidate, ImmutableArray<FormChoiceOption> options, out string code)
    {
        if (TryMatchExact(candidate, options, out code))
        {
            return true;
        }

        if (TryParseOrdinalPhrase(candidate, out var index) && index >= 1 && index <= options.Length)
        {
            code = options[index - 1].Code;
            return true;
        }

        code = string.Empty;
        return false;
    }

    public bool TryMatchExact(string candidate, ImmutableArray<FormChoiceOption> options, out string code)
    {
        foreach (var option in options)
        {
            if (string.Equals(NormalizeOption(option.DisplayName), candidate, StringComparison.Ordinal)
                || string.Equals(NormalizeOption(option.Code), candidate, StringComparison.Ordinal))
            {
                code = option.Code;
                return true;
            }
        }

        code = string.Empty;
        return false;
    }

    private string StripFillers(string? requestedIsoCode, string candidate)
    {
        var fillerTokens = _lexicon.GetFillerTokens(requestedIsoCode);
        if (fillerTokens.Count == 0)
        {
            return candidate;
        }

        var kept = candidate
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(token => !fillerTokens.Contains(token));
        return string.Join(' ', kept);
    }

    private bool TryParseOrdinalPhrase(string candidate, out int index)
    {
        index = 0;
        var tokens = candidate.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 1)
        {
            return TryResolveIndexToken(tokens[0], out index);
        }

        if (tokens.Length == 2 && OrdinalKeywords.Contains(tokens[0]))
        {
            return TryResolveIndexToken(tokens[1], out index);
        }

        return false;
    }

    private bool TryResolveIndexToken(string token, out int index)
    {
        if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var digit))
        {
            index = digit;
            return true;
        }

        if (OrdinalWords.TryGetValue(token, out var ordinal))
        {
            index = ordinal;
            return true;
        }

        if (_numberWordParser.TryParse(token, out var cardinal))
        {
            index = cardinal;
            return true;
        }

        index = 0;
        return false;
    }

    private static bool ContainsNegation(string candidate, IReadOnlySet<string> vetoTokens)
    {
        if (vetoTokens.Count == 0)
        {
            return false;
        }

        foreach (var token in candidate.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (vetoTokens.Contains(token))
            {
                return true;
            }
        }

        return false;
    }

    private static ImmutableArray<string> ContainmentHits(
        string candidate,
        ImmutableArray<FormChoiceOption> options,
        IReadOnlySet<string> vetoTokens)
    {
        var candidateTokens = candidate.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var hits = ImmutableArray.CreateBuilder<string>();
        foreach (var option in options)
        {
            var labelTokens = NormalizeOption(option.DisplayName).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (labelTokens.Length == 0)
            {
                continue;
            }

            if (CandidateWithinLabel(candidateTokens, labelTokens)
                || LabelWithinCandidate(labelTokens, candidateTokens, vetoTokens))
            {
                hits.Add(option.Code);
            }
        }

        return hits.ToImmutable();
    }

    // Jedes Kandidatenwort muss Präfix eines eigenen Bezeichnungswortes sein: So passt
    // „nord“ zu „nordstadt“ und „bayern“ zu „bayern freistaat“.
    private static bool CandidateWithinLabel(string[] candidateTokens, string[] labelTokens)
    {
        if (candidateTokens.Length == 0 || candidateTokens.Length > labelTokens.Length)
        {
            return false;
        }

        var consumed = new bool[labelTokens.Length];
        foreach (var candidateToken in candidateTokens)
        {
            var matched = false;
            for (var i = 0; i < labelTokens.Length; i++)
            {
                if (!consumed[i] && labelTokens[i].StartsWith(candidateToken, StringComparison.Ordinal))
                {
                    consumed[i] = true;
                    matched = true;
                    break;
                }
            }

            if (!matched)
            {
                return false;
            }
        }

        return true;
    }

    // Alle Bezeichnungswörter müssen im Kandidaten vorkommen. Zusätzliche Kandidatenwörter
    // sind nur als Negationswörter erlaubt. Diese sperrt bereits die vorgelagerte Prüfung.
    private static bool LabelWithinCandidate(string[] labelTokens, string[] candidateTokens, IReadOnlySet<string> vetoTokens)
    {
        var candidateSet = new HashSet<string>(candidateTokens, StringComparer.Ordinal);
        foreach (var labelToken in labelTokens)
        {
            if (!candidateSet.Contains(labelToken))
            {
                return false;
            }
        }

        var labelSet = new HashSet<string>(labelTokens, StringComparer.Ordinal);
        foreach (var candidateToken in candidateTokens)
        {
            if (!labelSet.Contains(candidateToken) && !vetoTokens.Contains(candidateToken))
            {
                return false;
            }
        }

        return true;
    }

    private static string NormalizeOption(string value)
        => TextNormalizer.NormalizeForPhraseMatching(value);

    private static Dictionary<string, int> BuildOrdinalWords()
    {
        var words = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var (stem, value) in OrdinalStems)
        {
            foreach (var ending in OrdinalDeclensionEndings)
            {
                words[stem + ending] = value;
            }
        }

        return words;
    }
}
