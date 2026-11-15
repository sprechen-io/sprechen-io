using System.Globalization;
using System.Text;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Bildet die Laufzeitnormalisierung von Auswahlnamen für die Vorprüfung nach.
/// Eine direkte Abhängigkeit von Speech ist hier nicht zulässig. Beide Implementierungen
/// müssen deshalb dieselben Vergleichsformen erzeugen.
/// </summary>
internal sealed class CatalogSelectionTextNormalizer
{
    internal string NormalizeForPhraseMatching(string raw)
    {
        var normalized = NormalizeDiacritics(raw);
        if (normalized.Length == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                continue;
            }

            if (builder.Length > 0 && builder[^1] != ' ')
            {
                builder.Append(' ');
            }
        }

        if (builder.Length > 0 && builder[^1] == ' ')
        {
            builder.Length--;
        }

        return builder.ToString();
    }

    internal string NormalizeForCompactMatching(string raw)
    {
        var normalized = NormalizeDiacritics(raw);
        if (normalized.Length == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    internal string NormalizeForIntentMatching(string raw, string language)
    {
        var normalized = NormalizeForPhraseMatching(raw);
        return language.StartsWith("de", StringComparison.OrdinalIgnoreCase)
            ? CollapseGermanAsciiTransliterations(normalized)
            : normalized;
    }

    internal static bool MatchesAsWordOrPhrase(string normalizedText, string normalizedPattern)
    {
        if (normalizedPattern.Length == 0)
        {
            return false;
        }

        var startIndex = 0;
        while (startIndex <= normalizedText.Length - normalizedPattern.Length)
        {
            var index = normalizedText.IndexOf(normalizedPattern, startIndex, StringComparison.Ordinal);
            if (index < 0)
            {
                return false;
            }

            var validStart = index == 0 || normalizedText[index - 1] == ' ';
            var endIndex = index + normalizedPattern.Length;
            var validEnd = endIndex == normalizedText.Length || normalizedText[endIndex] == ' ';
            if (validStart && validEnd)
            {
                return true;
            }

            startIndex = index + 1;
        }

        return false;
    }

    internal static bool StartsWithPhraseBoundary(string input, string prefix)
        => prefix.Length > 0
           && input.StartsWith(prefix, StringComparison.Ordinal)
           && (input.Length == prefix.Length || input[prefix.Length] == ' ');

    private static string NormalizeDiacritics(string raw)
    {
        ArgumentNullException.ThrowIfNull(raw);

        var trimmed = raw.Trim().ToLowerInvariant();
        if (trimmed.Length == 0)
        {
            return string.Empty;
        }

        var decomposed = trimmed.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string CollapseGermanAsciiTransliterations(string value)
    {
        if (value.Length < 2)
        {
            return value;
        }

        var builder = new StringBuilder(value.Length);
        for (var index = 0; index < value.Length; index++)
        {
            if (!ShouldCollapseGermanTransliteration(value, index))
            {
                builder.Append(value[index]);
                continue;
            }

            builder.Append(value[index]);
            index++;
        }

        return builder.ToString();
    }

    private static bool ShouldCollapseGermanTransliteration(string value, int index)
    {
        if (index + 1 >= value.Length
            || value[index] is not ('a' or 'o' or 'u')
            || value[index + 1] != 'e')
        {
            return false;
        }

        var previous = index > 0 ? value[index - 1] : (char?)null;
        var following = index + 2 < value.Length ? value[index + 2] : (char?)null;
        return !IsAsciiVowel(previous) && !IsAsciiVowel(following);
    }

    private static bool IsAsciiVowel(char? value)
        => value is 'a' or 'e' or 'i' or 'o' or 'u' or 'y';
}
