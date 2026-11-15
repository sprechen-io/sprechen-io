namespace Sprechen.Speech.LanguageContracts;

/// <summary>
/// Überträgt Sprachangebot und zugehörige Standardsprache als Grundlage des Anwendungskatalogs für Sprachen.
/// </summary>
public sealed record SupportedLanguages(
    string DefaultIsoCode,
    IReadOnlyList<SupportedLanguage> Languages);
