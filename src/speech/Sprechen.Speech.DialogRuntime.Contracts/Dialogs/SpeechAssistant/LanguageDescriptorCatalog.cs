namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet unterstützten Sprachcodes ihre Kultur und ihr Verhalten beim Zeitparsen zu.
/// <see cref="For"/> weist unbekannte Codes zurück. <c>TryGet</c> meldet sie mit
/// <c>false</c>. Beim ersten Zugriff prüft <see cref="Validate"/> die Vollständigkeit
/// gegen <see cref="LanguageDescriptors.AllIsoCodes"/>.
/// </summary>
public static class LanguageDescriptorCatalog
{
    private static readonly IReadOnlyDictionary<string, LanguageDescriptor> DescriptorsByIsoCode =
        new Dictionary<string, LanguageDescriptor>(StringComparer.Ordinal)
        {
            ["de"] = new LanguageDescriptor("de", "de-DE", UsesRomanceTemporalParsing: false),
            ["en"] = new LanguageDescriptor("en", "en-US", UsesRomanceTemporalParsing: false),
            ["fr"] = new LanguageDescriptor("fr", "fr-FR", UsesRomanceTemporalParsing: true),
            ["es"] = new LanguageDescriptor("es", "es-ES", UsesRomanceTemporalParsing: true),
            ["it"] = new LanguageDescriptor("it", "it-IT", UsesRomanceTemporalParsing: true),
        };

    static LanguageDescriptorCatalog()
    {
        Validate();
    }

    /// <summary>
    /// Löst einen unterstützten Sprachcode auf und wirft bei unbekannten Codes.
    /// Regions- und Schriftzusätze wie <c>de-DE</c> oder <c>de_DE</c> werden
    /// auf den Basiscode reduziert.
    /// </summary>
    public static LanguageDescriptor For(string isoCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);
        var normalized = NormalizeIsoCode(isoCode);
        return DescriptorsByIsoCode.TryGetValue(normalized, out var descriptor)
            ? descriptor
            : throw new InvalidOperationException($"No language descriptor is modeled for ISO code '{isoCode}'.");
    }

    public static bool TryGet(string? isoCode, out LanguageDescriptor descriptor)
    {
        descriptor = null!;
        if (string.IsNullOrWhiteSpace(isoCode))
        {
            return false;
        }

        return DescriptorsByIsoCode.TryGetValue(NormalizeIsoCode(isoCode), out descriptor!);
    }

    private static string NormalizeIsoCode(string isoCode)
    {
        var token = isoCode.Trim();
        var separatorIndex = token.IndexOfAny(['-', '_']);
        if (separatorIndex > 0)
        {
            token = token[..separatorIndex];
        }

        return token.ToLowerInvariant();
    }

    /// <summary>
    /// Prüft beim ersten Zugriff, dass jeder unterstützte Sprachcode genau einen
    /// Deskriptor besitzt und keine weiteren Codes eingetragen sind.
    /// </summary>
    public static void Validate()
    {
        var missing = LanguageDescriptors.AllIsoCodes
            .Where(isoCode => !DescriptorsByIsoCode.ContainsKey(isoCode))
            .ToArray();
        if (missing.Length > 0)
        {
            throw new InvalidOperationException(
                $"Supported ISO codes without a language descriptor: {string.Join(", ", missing)}.");
        }

        var dangling = DescriptorsByIsoCode.Keys
            .Where(isoCode => !LanguageDescriptors.AllIsoCodes.Contains(isoCode))
            .ToArray();
        if (dangling.Length > 0)
        {
            throw new InvalidOperationException(
                $"Language descriptors outside the supported ISO set: {string.Join(", ", dangling)}.");
        }
    }
}
