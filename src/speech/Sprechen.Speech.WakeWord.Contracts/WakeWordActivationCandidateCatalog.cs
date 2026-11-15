namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Vereinigt und normalisiert beide Bestände von Aktivierungsformen. Längere Tokenfolgen stehen zuerst, damit kürzere Präfixe sie nicht verdrängen.
/// </summary>
internal static class WakeWordActivationCandidateCatalog
{
    public static List<string[]> Build(WakeWordInventory wakeWordInventory)
    {
        ArgumentNullException.ThrowIfNull(wakeWordInventory);

        return wakeWordInventory.ActivationForms
            .Concat(wakeWordInventory.RecognitionOnlyActivationForms)
            .Select(TryTokenizeValue)
            .Where(static tokens => tokens is not null)
            .Select(static tokens => tokens!)
            .GroupBy(static tokens => string.Join(" ", tokens), StringComparer.Ordinal)
            .Select(static group => group.First())
            .OrderByDescending(static tokens => tokens.Length)
            .ThenBy(static tokens => string.Join(" ", tokens), StringComparer.Ordinal)
            .ToList();
    }

    private static string[]? TryTokenizeValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalizedValue = TextNormalizer.NormalizeForTokenComparison(value);
        var tokens = TextNormalizer.TokenizeLettersOrDigits(normalizedValue).ToArray();
        return tokens.Length == 0
            ? null
            : tokens;
    }
}
