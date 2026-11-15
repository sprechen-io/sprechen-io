namespace Sprechen.Speech.App.ClientInfrastructure.Services;

/// <summary>
/// Wählt für die Sprachinitialisierung die erste tatsächlich unterstützte Präferenz. Regionale Suffixe mit Bindestrich oder Unterstrich verhindern keinen Treffer.
/// </summary>
internal static class SupportedLanguageResolver
{
    public static string? ResolvePreferredIsoCode(IEnumerable<string?> languageTags)
    {
        ArgumentNullException.ThrowIfNull(languageTags);

        foreach (var languageTag in languageTags)
        {
            var resolvedIsoCode = ResolveIsoCode(languageTag);
            if (resolvedIsoCode is not null)
            {
                return resolvedIsoCode;
            }
        }

        return null;
    }

    public static string? ResolveIsoCode(string? languageTag)
    {
        if (string.IsNullOrWhiteSpace(languageTag))
        {
            return null;
        }

        var normalizedTag = languageTag.Trim();
        var separatorIndex = normalizedTag.IndexOfAny(['-', '_']);
        if (separatorIndex > 0)
        {
            normalizedTag = normalizedTag[..separatorIndex];
        }

        var normalizedIsoCode = IsoCode.TryNormalizeIso6391(normalizedTag);
        if (normalizedIsoCode is null)
        {
            return null;
        }

        return VoiceInterfaceModelCatalog.SupportedLanguages.Contains(normalizedIsoCode, StringComparer.Ordinal)
            ? normalizedIsoCode
            : null;
    }
}
