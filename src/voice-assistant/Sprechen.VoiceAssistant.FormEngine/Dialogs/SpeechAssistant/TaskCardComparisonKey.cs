using System.Globalization;
using System.Text;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Bildet den parameterfreien Vergleichsschlüssel einer Freitextvorgabe. Die
/// Schritte sind fest: Trim, NFC, Steuer- und Formatzeichen sowie kombinierende
/// Zeichen streichen, Kleinbuchstaben mit ß zu ss, Satzzeichen an den Rändern
/// streichen, Bindestriche zu Leerzeichen, danach jedes Leerzeichen streichen.
/// </summary>
internal static class TaskCardComparisonKey
{
    internal static string From(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var nfc = value.Trim().Normalize(NormalizationForm.FormC);
        var withoutMarks = new StringBuilder(nfc.Length);
        foreach (var character in nfc)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category is UnicodeCategory.Control or UnicodeCategory.Format or UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            withoutMarks.Append(character);
        }

        var folded = withoutMarks.ToString().ToLowerInvariant().Replace("ß", "ss", StringComparison.Ordinal);
        var withoutEdgePunctuation = StripEdgePunctuation(folded);
        var dashesAsSpace = ReplaceDashesWithSpace(withoutEdgePunctuation);
        return DeleteWhitespace(dashesAsSpace);
    }

    private static string StripEdgePunctuation(string value)
    {
        var start = 0;
        var end = value.Length;
        while (start < end && IsPunctuation(value[start]))
        {
            start++;
        }

        while (end > start && IsPunctuation(value[end - 1]))
        {
            end--;
        }

        return value[start..end];
    }

    private static string ReplaceDashesWithSpace(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (character == '\u2212' || CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.DashPunctuation)
            {
                builder.Append(' ');
                continue;
            }

            builder.Append(character);
        }

        return builder.ToString();
    }

    private static string DeleteWhitespace(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (char.IsWhiteSpace(character) || character == '\u00A0')
            {
                continue;
            }

            builder.Append(character);
        }

        return builder.ToString();
    }

    private static bool IsPunctuation(char character)
        => CharUnicodeInfo.GetUnicodeCategory(character) is
            UnicodeCategory.ConnectorPunctuation or
            UnicodeCategory.DashPunctuation or
            UnicodeCategory.OpenPunctuation or
            UnicodeCategory.ClosePunctuation or
            UnicodeCategory.InitialQuotePunctuation or
            UnicodeCategory.FinalQuotePunctuation or
            UnicodeCategory.OtherPunctuation;
}
