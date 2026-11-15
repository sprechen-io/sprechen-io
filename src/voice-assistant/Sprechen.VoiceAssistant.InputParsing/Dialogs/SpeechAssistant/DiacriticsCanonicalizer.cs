using System.Globalization;
using System.Text;

namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Wendet die gewählte Diakritikstrategie auf die Zeichen eines Eingabewerts an.
/// </summary>
internal static class DiacriticsCanonicalizer
{
    public static string ApplyStrategy(string value, string strategy)
    {
        ArgumentNullException.ThrowIfNull(value);

        var normalizedStrategy = DiacriticsStrategies.NormalizeOrDefault(strategy);
        if (value.Length == 0)
        {
            return value;
        }

        return normalizedStrategy switch
        {
            DiacriticsStrategies.PreferAscii => ToAscii(value),
            DiacriticsStrategies.PreferDiacritics => ToPreferredDiacritics(value),
            _ => value
        };
    }

    private static string ToAscii(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var replaced = value
            .Replace("\u00E4", "ae", StringComparison.Ordinal)
            .Replace("\u00F6", "oe", StringComparison.Ordinal)
            .Replace("\u00FC", "ue", StringComparison.Ordinal)
            .Replace("\u00C4", "Ae", StringComparison.Ordinal)
            .Replace("\u00D6", "Oe", StringComparison.Ordinal)
            .Replace("\u00DC", "Ue", StringComparison.Ordinal)
            .Replace("\u00DF", "ss", StringComparison.Ordinal);

        var decomposed = replaced.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) is UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(ch);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string ToPreferredDiacritics(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return GermanUmlautTransliterator.RestorePreferredDiacritics(value);
    }
}


