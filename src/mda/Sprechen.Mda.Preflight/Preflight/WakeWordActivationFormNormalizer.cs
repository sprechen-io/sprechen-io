using System.Globalization;
using System.Text;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Bildet Vergleichsformen aus kleingeschriebenen Buchstaben und Ziffern
/// ohne kombinierende Akzentzeichen. Andere Zeichen trennen Wörter.
/// </summary>
internal sealed class WakeWordActivationFormNormalizer
{
    public string[] NormalizeInventory(IReadOnlyList<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        return values
            .Select(NormalizeValueKey)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
    }

    public string? NormalizeValueKey(string? value)
    {
        var tokens = NormalizeTokens(value);
        return tokens.Length == 0
            ? null
            : string.Join(" ", tokens);
    }

    public string[] NormalizeTokens(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        var normalized = NormalizeText(value);
        if (normalized.Length == 0)
        {
            return [];
        }

        return SplitTokens(normalized);
    }

    private static string[] SplitTokens(string normalized)
    {
        var builder = new StringBuilder(normalized.Length);
        var tokens = new List<string>();

        foreach (var ch in normalized)
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
                continue;
            }

            FlushToken(builder, tokens);
        }

        FlushToken(builder, tokens);

        return tokens.ToArray();
    }

    private static void FlushToken(StringBuilder builder, List<string> tokens)
    {
        if (builder.Length == 0)
        {
            return;
        }

        tokens.Add(builder.ToString());
        builder.Clear();
    }

    private static string NormalizeText(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var trimmed = value.Trim().ToLowerInvariant();
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
