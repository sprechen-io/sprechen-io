namespace Sprechen.Service.Core.Services;

/// <summary>
/// Ergänzt einen Sprachcode um seinen Katalognamen. Bei fehlender Zuordnung bleibt der Code als Anzeige erhalten.
/// </summary>
internal sealed class SupportedLanguageDisplayResolver
{
    private readonly ILanguageCatalog _languageCatalog;

    public SupportedLanguageDisplayResolver(ILanguageCatalog languageCatalog)
    {
        _languageCatalog = languageCatalog ?? throw new ArgumentNullException(nameof(languageCatalog));
    }

    public SupportedLanguage Resolve(string isoCode)
    {
        if (!LanguageCode.TryParseIso6391(isoCode, out var code))
        {
            return new SupportedLanguage(isoCode, isoCode);
        }

        return new SupportedLanguage(isoCode, _languageCatalog.GetEndonym(code));
    }
}
