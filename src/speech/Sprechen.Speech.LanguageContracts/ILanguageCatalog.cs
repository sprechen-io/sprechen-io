namespace Sprechen.Speech.LanguageContracts;

/// <summary>
/// Stellt verfügbare Sprachen und lokalisierte Bezeichnungen als gemeinsame Grundlage für Sprachprüfung und Ansagen bereit.
/// </summary>
public interface ILanguageCatalog
{
    LanguageCode DefaultLanguage { get; }

    IReadOnlyList<LanguageCode> SupportedLanguages { get; }

    bool IsSupported(LanguageCode language);

    string GetEndonym(LanguageCode language);

    string GetDisplayName(LanguageCode language, LanguageCode displayLanguage);
}



