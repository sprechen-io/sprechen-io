using System.Text;

namespace Sprechen.Speech.TextNormalization;

/// <summary>
/// Entfernt Diakritika, normalisiert auf Kleinschreibung und behält
/// für kompakte Vergleiche ausschließlich Buchstaben und Ziffern.
/// </summary>
internal static class CompactTextNormalizer
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
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
