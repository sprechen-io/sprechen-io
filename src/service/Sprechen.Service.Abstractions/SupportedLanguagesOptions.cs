namespace Sprechen.Service.Abstractions;

/// <summary>
/// Konfiguration der unterstützten Sprachen und der Standardsprache.
/// </summary>
public sealed class SupportedLanguagesOptions
{
    public const string SectionName = "SupportedLanguages";

    public static IReadOnlyList<string> DefaultIsoCodes { get; } = ["de", "en", "fr", "es", "it"];

    public static string AllowedIsoCodesDisplay => string.Join(",", DefaultIsoCodes);

    public static IReadOnlySet<string> AllowedIsoCodes { get; } =
        new HashSet<string>(DefaultIsoCodes, StringComparer.OrdinalIgnoreCase);

    public string DefaultIsoCode { get; init; } = "de";

    public string[] IsoCodes { get; init; } = [.. DefaultIsoCodes];
}
