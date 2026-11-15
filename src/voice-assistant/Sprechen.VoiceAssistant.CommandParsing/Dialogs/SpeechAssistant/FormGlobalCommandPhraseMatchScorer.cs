namespace Sprechen.VoiceAssistant.CommandParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Bewertet vollständige Befehlsphrasen anhand ihrer Position und zusätzlicher Wörter im Transkript.
/// </summary>
internal static class FormGlobalCommandPhraseMatchScorer
{
    public static bool TryScore(
        string normalizedTranscript,
        string normalizedUtterance,
        FormGlobalCommandParserOptions options,
        out double score)
    {
        score = 0d;
        var matchIndex = FindWordOrPhraseMatchIndex(normalizedTranscript, normalizedUtterance);
        if (matchIndex < 0)
        {
            return false;
        }

        score = ScorePhraseMatch(normalizedTranscript, normalizedUtterance, matchIndex, options);
        return true;
    }

    private static int FindWordOrPhraseMatchIndex(string normalizedText, string normalizedPattern)
    {
        if (normalizedPattern.Length == 0)
        {
            return -1;
        }

        var startIndex = 0;
        while (TryFindNext(normalizedText, normalizedPattern, startIndex, out var index))
        {
            if (IsWholeWordOrPhraseMatch(normalizedText, normalizedPattern, index))
            {
                return index;
            }

            startIndex = index + 1;
        }

        return -1;
    }

    private static bool TryFindNext(
        string normalizedText,
        string normalizedPattern,
        int startIndex,
        out int index)
    {
        index = normalizedText.IndexOf(normalizedPattern, startIndex, StringComparison.Ordinal);
        return index >= 0;
    }

    private static bool IsWholeWordOrPhraseMatch(
        string normalizedText,
        string normalizedPattern,
        int index)
    {
        var validStart = index == 0 || normalizedText[index - 1] == ' ';
        var endIndex = index + normalizedPattern.Length;
        var validEnd = endIndex == normalizedText.Length || normalizedText[endIndex] == ' ';
        return validStart && validEnd;
    }

    private static double ScorePhraseMatch(
        string normalizedTranscript,
        string normalizedUtterance,
        int matchIndex,
        FormGlobalCommandParserOptions options)
    {
        var score = options.ExactMatchScore;
        if (!string.Equals(normalizedTranscript, normalizedUtterance, StringComparison.Ordinal))
        {
            score = ScorePartialPhraseMatch(normalizedTranscript, normalizedUtterance, matchIndex, options);
        }

        return Math.Clamp(score, 0d, options.ExactMatchScore);
    }

    private static double ScorePartialPhraseMatch(
        string normalizedTranscript,
        string normalizedUtterance,
        int matchIndex,
        FormGlobalCommandParserOptions options)
    {
        var utteranceEnd = matchIndex + normalizedUtterance.Length;
        var score = matchIndex == 0
            ? options.PrefixPhraseScore
            : (utteranceEnd == normalizedTranscript.Length ? options.SuffixPhraseScore : options.InfixPhraseScore);

        ApplyExtraTokenPenalties(
            normalizedTranscript,
            normalizedUtterance,
            matchIndex,
            options,
            ref score);

        return score;
    }

    private static void ApplyExtraTokenPenalties(
        string normalizedTranscript,
        string normalizedUtterance,
        int matchIndex,
        FormGlobalCommandParserOptions options,
        ref double score)
    {
        var transcriptTokens = normalizedTranscript.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var utteranceTokens = normalizedUtterance.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (transcriptTokens.Length <= utteranceTokens.Length || utteranceTokens.Length == 0)
        {
            return;
        }

        ApplyLeadingTokenPenalties(transcriptTokens, normalizedTranscript, matchIndex, options, ref score);
        ApplyTrailingTokenPenalties(transcriptTokens, normalizedTranscript, normalizedUtterance, matchIndex, options, ref score);
    }

    private static void ApplyLeadingTokenPenalties(
        string[] transcriptTokens,
        string normalizedTranscript,
        int matchIndex,
        FormGlobalCommandParserOptions options,
        ref double score)
    {
        var leadingTokenCount = CountTokensBeforeIndex(normalizedTranscript, matchIndex);
        for (var i = 0; i < leadingTokenCount; i++)
        {
            score -= GetExtraTokenPenalty(transcriptTokens[i], options) + options.LeadingExtraTokenPenalty;
        }
    }

    private static void ApplyTrailingTokenPenalties(
        string[] transcriptTokens,
        string normalizedTranscript,
        string normalizedUtterance,
        int matchIndex,
        FormGlobalCommandParserOptions options,
        ref double score)
    {
        var leadingTokenCount = CountTokensBeforeIndex(normalizedTranscript, matchIndex);
        var utteranceTokens = normalizedUtterance.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var trailingStart = leadingTokenCount + utteranceTokens.Length;
        for (var i = trailingStart; i < transcriptTokens.Length; i++)
        {
            score -= GetExtraTokenPenalty(transcriptTokens[i], options);
        }
    }

    private static int CountTokensBeforeIndex(string normalizedTranscript, int index)
    {
        if (index <= 0)
        {
            return 0;
        }

        return normalizedTranscript[..index].Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private static double GetExtraTokenPenalty(string token, FormGlobalCommandParserOptions options)
        => options.PoliteModifierTokens.Contains(token)
            ? options.PoliteExtraTokenPenalty
            : options.UnknownExtraTokenPenalty;
}
