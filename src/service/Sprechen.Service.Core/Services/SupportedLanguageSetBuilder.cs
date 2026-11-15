namespace Sprechen.Service.Core.Services;

/// <summary>
/// Erstellt die unterstützte Sprachmenge aus der Konfiguration, normalisiert und entfernt Duplikate
/// und ergänzt die Standardsprache, falls sie fehlt.
/// </summary>
internal sealed class SupportedLanguageSetBuilder
{
    public SupportedLanguageSet Build(SupportedLanguagesOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var defaultIsoCode = NormalizeAllowedIsoCode(options.DefaultIsoCode)
            ?? throw new InvalidOperationException($"{SupportedLanguagesOptions.SectionName}:DefaultIsoCode must be a valid ISO 639-1 code (e.g. 'de').");

        var resolved = ResolveConfiguredIsoCodes(options.IsoCodes, defaultIsoCode);
        return new SupportedLanguageSet(defaultIsoCode, resolved);
    }

    private static IReadOnlyList<string> ResolveConfiguredIsoCodes(
        IReadOnlyList<string>? rawIsoCodes,
        string defaultIsoCode)
    {
        var resolved = CollectConfiguredIsoCodes(rawIsoCodes);
        EnsureDefaultIsoCode(resolved, defaultIsoCode);
        return resolved.ToArray();
    }

    private static List<string> CollectConfiguredIsoCodes(IReadOnlyList<string>? rawIsoCodes)
    {
        var resolved = new List<string>();
        var uniqueIsoCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var raw in rawIsoCodes ?? Array.Empty<string>())
        {
            var iso = NormalizeAllowedIsoCode(raw);
            if (iso is not null && uniqueIsoCodes.Add(iso))
            {
                resolved.Add(iso);
            }
        }

        return resolved;
    }

    private static void EnsureDefaultIsoCode(List<string> resolved, string defaultIsoCode)
    {
        if (resolved.Count == 0)
        {
            resolved.Add(defaultIsoCode);
            return;
        }

        if (!resolved.Contains(defaultIsoCode, StringComparer.OrdinalIgnoreCase))
        {
            resolved.Insert(0, defaultIsoCode);
        }
    }

    private static string? NormalizeAllowedIsoCode(string? rawIsoCode)
    {
        var iso = LanguageCodeNormalizer.NormalizeIso6391(rawIsoCode);
        if (iso is null)
        {
            return null;
        }

        if (!SupportedLanguagesOptions.AllowedIsoCodes.Contains(iso))
        {
            throw new InvalidOperationException(
                $"Supported language '{iso}' is not allowed. Allowed: {string.Join(", ", SupportedLanguagesOptions.AllowedIsoCodes)}.");
        }

        return iso;
    }
}
