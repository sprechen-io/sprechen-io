using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Ermittelt die gewünschte Anredeform aus sprachbezogenem Wortschatz und bereinigter Äußerung, damit ein Anredewechsel ein aufgelöstes Ziel erhält.
/// </summary>
internal sealed class FormVoiceCommandPolitenessResolver
{
    private static readonly ImmutableHashSet<string> LeadingNoiseTokens =
        ImmutableHashSet.Create(
            StringComparer.Ordinal,
            "a",
            "al",
            "anrede",
            "ansprache",
            "auf",
            "bitte",
            "courtoisie",
            "cortesia",
            "de",
            "del",
            "des",
            "di",
            "forme",
            "la",
            "le",
            "mode",
            "modo",
            "please",
            "politesse",
            "politeness",
            "to",
            "tratamiento",
            "zu");

    public bool HasPolitenessProfile(string isoCode)
        => VoiceInterfaceModelCatalog.Lexicon.Politeness.HasProfile(isoCode);

    public string FormatSupportedOptions(string isoCode)
    {
        var normalizedIsoCode = IsoCode.TryNormalizeIso6391(isoCode);
        if (normalizedIsoCode is null || !HasPolitenessProfile(normalizedIsoCode))
        {
            return string.Empty;
        }

        return string.Join(
            ", ",
            new[]
            {
                VoiceInterfaceModelCatalog.Lexicon.Politeness.GetFormalWord(normalizedIsoCode),
                VoiceInterfaceModelCatalog.Lexicon.Politeness.GetInformalWord(normalizedIsoCode)
            });
    }

    public bool TryResolve(
        string? rawArgument,
        string isoCode,
        out PolitenessLevel targetPolitenessLevel,
        out string displayLabel)
    {
        targetPolitenessLevel = default;
        displayLabel = string.Empty;

        var normalizedIsoCode = IsoCode.TryNormalizeIso6391(isoCode);
        if (normalizedIsoCode is null || !HasPolitenessProfile(normalizedIsoCode))
        {
            return false;
        }

        var normalizedCandidate = NormalizeCandidate(rawArgument);
        if (normalizedCandidate.Length == 0)
        {
            return false;
        }

        if (MatchesAny(
                normalizedCandidate,
                VoiceInterfaceModelCatalog.Lexicon.Politeness.GetFormalWord(normalizedIsoCode),
                VoiceInterfaceModelCatalog.Lexicon.Politeness.GetFormalTokens(normalizedIsoCode)))
        {
            targetPolitenessLevel = PolitenessLevel.Formal;
            displayLabel = VoiceInterfaceModelCatalog.Lexicon.Politeness.GetFormalWord(normalizedIsoCode);
            return true;
        }

        if (MatchesAny(
                normalizedCandidate,
                VoiceInterfaceModelCatalog.Lexicon.Politeness.GetInformalWord(normalizedIsoCode),
                VoiceInterfaceModelCatalog.Lexicon.Politeness.GetInformalTokens(normalizedIsoCode)))
        {
            targetPolitenessLevel = PolitenessLevel.Informal;
            displayLabel = VoiceInterfaceModelCatalog.Lexicon.Politeness.GetInformalWord(normalizedIsoCode);
            return true;
        }

        return false;
    }

    private static bool MatchesAny(
        string normalizedCandidate,
        string displayLabel,
        IReadOnlyCollection<string> tokens)
    {
        var candidates = new HashSet<string>(StringComparer.Ordinal)
        {
            TextNormalizer.NormalizeForPhraseMatching(displayLabel)
        };

        foreach (var token in tokens)
        {
            candidates.Add(TextNormalizer.NormalizeForPhraseMatching(token));
        }

        return candidates.Contains(normalizedCandidate);
    }

    private static string NormalizeCandidate(string? rawArgument)
    {
        if (string.IsNullOrWhiteSpace(rawArgument))
        {
            return string.Empty;
        }

        var normalized = TextNormalizer.NormalizeForPhraseMatching(rawArgument);
        if (normalized.Length == 0)
        {
            return string.Empty;
        }

        var tokens = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var start = 0;

        while (start < tokens.Length && LeadingNoiseTokens.Contains(tokens[start]))
        {
            start++;
        }

        return start >= tokens.Length
            ? string.Empty
            : string.Join(' ', tokens[start..]);
    }
}
