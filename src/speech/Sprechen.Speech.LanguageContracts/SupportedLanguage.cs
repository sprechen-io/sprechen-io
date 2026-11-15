namespace Sprechen.Speech.LanguageContracts;

/// <summary>
/// Verknüpft eine unterstützte Sprachkennung mit ihrem Anzeigenamen für Sprachkatalog und Sprachauswahl.
/// </summary>
public sealed record SupportedLanguage(
    string IsoCode,
    string DisplayName);
