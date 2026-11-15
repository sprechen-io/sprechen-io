namespace Sprechen.Speech.LanguageContracts;

/// <summary>
/// Vergleicht Kandidaten mit Sprachcodes und normalisierten Namen als Grundlage der Auflösung eines Sprachwechselziels.
/// </summary>
internal sealed class LanguageTargetCandidateMatcher
{
    /// <summary>
    /// Die Mindestlänge verhindert, dass kurze Sprachcodes durch eine einzelne Zeichenänderung verwechselt werden.
    /// </summary>
    private const int MinimumLengthForEditTolerance = 5;

    private readonly ILanguageCatalog _languageCatalog;

    public LanguageTargetCandidateMatcher(ILanguageCatalog languageCatalog)
    {
        _languageCatalog = languageCatalog ?? throw new ArgumentNullException(nameof(languageCatalog));
    }

    public bool Matches(
        LanguageCode targetLanguage,
        LanguageCode displayLanguage,
        string normalizedCandidate)
    {
        var seenVariants = new HashSet<string>(StringComparer.Ordinal);
        foreach (var variant in GetCandidateVariants(targetLanguage, displayLanguage))
        {
            if (!seenVariants.Add(variant))
            {
                continue;
            }

            if (string.Equals(normalizedCandidate, variant, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Prüft, ob ein ausreichend langer Kandidat höchstens eine Zeichenänderung vom Sprachnamen entfernt ist.
    /// </summary>
    /// <remarks>
    /// Kandidat und Namensvariante müssen jeweils mindestens fünf Zeichen lang sein.
    /// Die Eindeutigkeit über mehrere Sprachen prüft der aufrufende Zielresolver.
    /// </remarks>
    public bool MatchesWithinOneEdit(
        LanguageCode targetLanguage,
        LanguageCode displayLanguage,
        string normalizedCandidate)
    {
        if (normalizedCandidate.Length < MinimumLengthForEditTolerance)
        {
            return false;
        }

        var seenVariants = new HashSet<string>(StringComparer.Ordinal);
        foreach (var variant in GetCandidateVariants(targetLanguage, displayLanguage))
        {
            if (!seenVariants.Add(variant) || variant.Length < MinimumLengthForEditTolerance)
            {
                continue;
            }

            if (IsWithinOneEdit(normalizedCandidate, variant))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Vergleicht Zeichenfolgen mit höchstens einer Ersetzung, Einfügung oder Löschung.
    /// </summary>
    private static bool IsWithinOneEdit(string candidate, string variant)
    {
        var lengthDifference = candidate.Length - variant.Length;
        if (lengthDifference is < -1 or > 1)
        {
            return false;
        }

        var candidateIndex = 0;
        var variantIndex = 0;
        var edits = 0;
        while (candidateIndex < candidate.Length && variantIndex < variant.Length)
        {
            if (candidate[candidateIndex] == variant[variantIndex])
            {
                candidateIndex++;
                variantIndex++;
                continue;
            }

            if (++edits > 1)
            {
                return false;
            }

            if (lengthDifference == 0)
            {
                candidateIndex++;
                variantIndex++;
            }
            else if (lengthDifference > 0)
            {
                candidateIndex++;
            }
            else
            {
                variantIndex++;
            }
        }

        return edits + (candidate.Length - candidateIndex) + (variant.Length - variantIndex) <= 1;
    }

    internal IEnumerable<string> GetCandidateVariants(LanguageCode targetLanguage, LanguageCode displayLanguage)
    {
        yield return targetLanguage.Value;
        yield return TextNormalizer.NormalizeForPhraseMatching(_languageCatalog.GetEndonym(targetLanguage));

        foreach (var supportedDisplayLanguage in _languageCatalog.SupportedLanguages)
        {
            yield return TextNormalizer.NormalizeForPhraseMatching(
                _languageCatalog.GetDisplayName(targetLanguage, supportedDisplayLanguage));
        }

        yield return TextNormalizer.NormalizeForPhraseMatching(
            _languageCatalog.GetDisplayName(targetLanguage, displayLanguage));
    }
}
