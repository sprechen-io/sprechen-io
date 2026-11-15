using System.Globalization;
using System.Text;

namespace Sprechen.Speech.TextNormalization;

/// <summary>
/// Entfernt nach kanonischer Zerlegung nichtabstandsbildende kombinierende Zeichen
/// und liefert kleingeschriebenen Text ohne äußere Leerzeichen in zusammengesetzter Unicode-Form.
/// Dies ist die gemeinsame Vorstufe für wortgrenzenerhaltende und kompakte Textvergleiche.
/// </summary>
internal static class TextDiacriticsNormalizer
{
    public static string Normalize(string raw)
    {
        ArgumentNullException.ThrowIfNull(raw);

        var trimmed = raw.Trim().ToLowerInvariant();
        if (trimmed.Length == 0)
        {
            return string.Empty;
        }

        var decomposed = trimmed.Normalize(NormalizationForm.FormD);
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
}
