namespace Sprechen.VoiceAssistant.CommandParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Erkennt vollständige Befehlsphrasen, die ausschließlich von Höflichkeitswörtern umgeben sind.
/// </summary>
internal static class FormGlobalCommandIsolationMatcher
{
    internal static bool IsUtteranceWithOnlyPolitePadding(
        string normalizedTranscript,
        string normalizedUtterance,
        IReadOnlySet<string> politeModifierTokens)
    {
        var transcriptTokens = normalizedTranscript.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var utteranceTokens = normalizedUtterance.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (utteranceTokens.Length == 0 || transcriptTokens.Length < utteranceTokens.Length)
        {
            return false;
        }

        for (var start = 0; start <= transcriptTokens.Length - utteranceTokens.Length; start++)
        {
            if (!MatchesUtteranceAt(transcriptTokens, utteranceTokens, start))
            {
                continue;
            }

            if (HasOnlyPoliteModifierTokens(transcriptTokens, 0, start, politeModifierTokens)
                && HasOnlyPoliteModifierTokens(
                    transcriptTokens,
                    start + utteranceTokens.Length,
                    transcriptTokens.Length,
                    politeModifierTokens))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesUtteranceAt(string[] transcriptTokens, string[] utteranceTokens, int start)
    {
        for (var tokenIndex = 0; tokenIndex < utteranceTokens.Length; tokenIndex++)
        {
            if (!string.Equals(
                    transcriptTokens[start + tokenIndex],
                    utteranceTokens[tokenIndex],
                    StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasOnlyPoliteModifierTokens(
        string[] tokens,
        int startInclusive,
        int endExclusive,
        IReadOnlySet<string> politeModifierTokens)
    {
        for (var index = startInclusive; index < endExclusive; index++)
        {
            if (!politeModifierTokens.Contains(tokens[index]))
            {
                return false;
            }
        }

        return true;
    }
}
