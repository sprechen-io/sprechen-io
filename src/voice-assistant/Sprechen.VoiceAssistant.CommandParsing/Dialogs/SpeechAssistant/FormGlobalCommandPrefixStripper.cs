using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.CommandParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Entfernt lokalisierte Befehlspräfixe, sofern ein anschließender Befehlsinhalt vorhanden ist.
/// </summary>
internal static class FormGlobalCommandPrefixStripper
{
    internal static bool TryStripPrefix(
        string transcript,
        IEnumerable<ResolvedGlobalCommandPrefixDefinition> prefixes,
        string isoCode,
        PolitenessLevel politenessLevel,
        out string strippedTranscript)
    {
        strippedTranscript = string.Empty;

        var normalizedTranscript = TextNormalizer.NormalizeForPhraseMatching(transcript);
        if (normalizedTranscript.Length == 0)
        {
            return false;
        }

        foreach (var prefix in prefixes)
        {
            foreach (var utterance in prefix.Utterances)
            {
                if (!TryStripPrefixUtterance(
                        normalizedTranscript,
                        prefix,
                        utterance,
                        isoCode,
                        politenessLevel,
                        out strippedTranscript))
                {
                    continue;
                }

                return true;
            }
        }

        return false;
    }

    private static bool TryStripPrefixUtterance(
        string normalizedTranscript,
        ResolvedGlobalCommandPrefixDefinition prefix,
        IReadOnlyDictionary<string, string> utterance,
        string isoCode,
        PolitenessLevel politenessLevel,
        out string strippedTranscript)
    {
        strippedTranscript = string.Empty;

        if (!FormGlobalCommandUtteranceResolver.TryResolve(prefix, utterance, isoCode, politenessLevel, out var resolvedPrefix)
            || string.IsNullOrWhiteSpace(resolvedPrefix))
        {
            return false;
        }

        var normalizedPrefix = TextNormalizer.NormalizeForPhraseMatching(resolvedPrefix);
        if (normalizedPrefix.Length == 0)
        {
            return false;
        }

        if (normalizedTranscript.Equals(normalizedPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        if (!normalizedTranscript.StartsWith(normalizedPrefix + " ", StringComparison.Ordinal))
        {
            return false;
        }

        var candidate = normalizedTranscript[normalizedPrefix.Length..].Trim();
        if (candidate.Length == 0)
        {
            return false;
        }

        strippedTranscript = candidate;
        return true;
    }
}
