using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Sprechen.Speech.TextNormalization;

/// <summary>
/// Normalisiert Wortfolgen mit kulturunabhängiger Kleinschreibung und
/// vereinheitlichten Abständen, erhält aber zusammengesetzte Akzente für unterscheidende Vergleiche.
/// </summary>
/// <remarks>
/// <para>Spanisches "sí" und italienisches "sì" unterscheiden sich durch ihren Akzent.
/// Sein Entfernen würde diese beiden Wörter gleichsetzen. Dieser Vergleich bewahrt ihn
/// gezielt, während die übrigen Normalisierungswege Diakritika entfernen.</para>
/// <para>Die diakritikentfernende Normalisierung macht "München" zu "munchen".
/// Eine Umschrift wie "Muenchen" wird dabei nicht zu demselben Wort. Auch dieser
/// akzenterhaltende Weg führt keine solche Umschrift durch.</para>
/// <para>Kleinschreibung und kanonische Zusammensetzung verwenden keine
/// Threadkultur. Satzzeichen werden zu zusammengefassten Wortabständen.</para>
/// </remarks>
internal static partial class AccentSensitivePhraseTextNormalizer
{
    public static string Normalize(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var lowered = raw.ToLowerInvariant().Normalize(NormalizationForm.FormC);

        var builder = new StringBuilder(lowered.Length);
        foreach (var ch in lowered)
        {
            builder.Append(char.IsLetterOrDigit(ch) ? ch : ' ');
        }

        return WhitespaceRegex().Replace(builder.ToString(), " ").Trim();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
