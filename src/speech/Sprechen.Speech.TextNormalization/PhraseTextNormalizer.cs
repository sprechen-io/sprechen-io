using System.Text;
using System.Text.RegularExpressions;

namespace Sprechen.Speech.TextNormalization;

/// <summary>
/// Entfernt Diakritika und normalisiert auf Kleinschreibung. Andere Zeichen
/// als Buchstaben und Ziffern werden zu zusammengefassten Wortabständen.
/// </summary>
internal static partial class PhraseTextNormalizer
{
    public static string Normalize(string raw)
    {
        var normalized = TextDiacriticsNormalizer.Normalize(raw);
        if (normalized.Length == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            builder.Append(char.IsLetterOrDigit(ch) ? ch : ' ');
        }

        return WhitespaceRegex().Replace(builder.ToString(), " ").Trim();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
