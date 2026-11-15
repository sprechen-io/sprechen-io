namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Vergleicht vollständige Aktivierungsformen an einer Tokenposition, damit Wortteile keine Aktivierung auslösen.
/// </summary>
internal static class WakeWordTokenSequenceMatcher
{
    public static int FindLengthAt(
        string[] transcriptTokens,
        int startIndex,
        IReadOnlyList<string[]> activationCandidates)
    {
        foreach (var activationTokens in activationCandidates)
        {
            if (Matches(transcriptTokens, startIndex, activationTokens))
            {
                return activationTokens.Length;
            }
        }

        return 0;
    }

    private static bool Matches(string[] transcriptTokens, int startIndex, string[] patternTokens)
    {
        ArgumentNullException.ThrowIfNull(transcriptTokens);
        ArgumentNullException.ThrowIfNull(patternTokens);

        var remaining = transcriptTokens.Length - startIndex;
        if (startIndex < 0 || remaining < patternTokens.Length)
        {
            return false;
        }

        for (var index = 0; index < patternTokens.Length; index++)
        {
            if (!string.Equals(transcriptTokens[startIndex + index], patternTokens[index], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}
