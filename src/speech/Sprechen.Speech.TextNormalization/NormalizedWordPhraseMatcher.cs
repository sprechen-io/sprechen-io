namespace Sprechen.Speech.TextNormalization;

/// <summary>
/// Sucht ein Muster ordinal in bereits normalisiertem Text und akzeptiert
/// es nur an Leerzeichen- oder Textgrenzen als vollständiges Wort beziehungsweise Wortfolge.
/// </summary>
internal static class NormalizedWordPhraseMatcher
{
    public static bool Matches(string normalizedText, string normalizedPattern)
    {
        if (string.IsNullOrEmpty(normalizedPattern))
        {
            return false;
        }

        var startIndex = 0;
        while (TryFindNext(normalizedText, normalizedPattern, startIndex, out var index))
        {
            if (IsWholeWordOrPhraseMatch(normalizedText, normalizedPattern, index))
            {
                return true;
            }

            startIndex = index + 1;
        }

        return false;
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
}
