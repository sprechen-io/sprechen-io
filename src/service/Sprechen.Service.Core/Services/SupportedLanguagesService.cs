using Microsoft.Extensions.Options;

namespace Sprechen.Service.Core.Services;

/// <summary>
/// Stellt konfigurierte Sprachen mit Anzeigenamen bereit und prüft angeforderte Sprachcodes.
/// </summary>
internal sealed class SupportedLanguagesService : ISupportedLanguagesService
{
    private readonly SupportedLanguageSet _languageSet;
    private readonly SupportedLanguageCodeResolver _languageCodes;

    public IReadOnlyList<SupportedLanguage> Languages { get; }

    public string DefaultIsoCode => _languageSet.DefaultIsoCode;

    public SupportedLanguagesService(IOptions<SupportedLanguagesOptions> options, ILanguageCatalog languageCatalog)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(languageCatalog);

        _languageSet = new SupportedLanguageSetBuilder().Build(options.Value);
        _languageCodes = new SupportedLanguageCodeResolver(_languageSet);

        var displays = new SupportedLanguageDisplayResolver(languageCatalog);
        Languages = _languageSet.IsoCodes
            .Select(displays.Resolve)
            .ToArray();
    }

    public bool IsSupportedIsoCode(string isoCode)
    {
        return _languageSet.Contains(isoCode);
    }

    public string GetEffectiveIsoCode(string? rawLanguageCode)
    {
        return _languageCodes.ResolveRequired(rawLanguageCode);
    }
}
