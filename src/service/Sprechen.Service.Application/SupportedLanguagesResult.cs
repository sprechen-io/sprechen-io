namespace Sprechen.Service.Application;

/// <summary>
/// Ergebnis der Sprachenabfrage mit Standardsprache und unterstützten Sprachen.
/// </summary>
public sealed record SupportedLanguagesResult(string DefaultIsoCode, IReadOnlyList<SupportedLanguage> Languages);
