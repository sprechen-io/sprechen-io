namespace Sprechen.Service.Abstractions;

/// <summary>
/// Liefert den Eigennamen einer Sprache in dieser Sprache.
/// </summary>
public interface ILanguageCatalog
{
    bool TryGetEndonym(LanguageCode language, out string endonym);

    string GetEndonym(LanguageCode language);
}

