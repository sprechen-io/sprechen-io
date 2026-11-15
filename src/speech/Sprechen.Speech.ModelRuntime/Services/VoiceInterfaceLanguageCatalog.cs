using Sprechen.Speech.App.Contracts.Dialogs.VoiceInterface;

namespace Sprechen.Speech.ModelRuntime.Services;

/// <summary>
/// Stellt unterstützte Sprachen, ihre Eigenbezeichnungen und lokalisierte
/// Anzeigenamen bereit und bestimmt eine gültige Standardsprache aus Katalog und Modell.
/// </summary>
public sealed class VoiceInterfaceLanguageCatalog : ILanguageCatalog
{
    private static readonly VoiceLanguageNameResourceIdResolver LanguageNameResourceIdResolver = new();

    private readonly Dictionary<LanguageCode, string> _endonyms;
    private readonly Dictionary<(LanguageCode language, LanguageCode displayLanguage), string> _displayNames;

    public VoiceInterfaceLanguageCatalog(SupportedLanguages supportedLanguages)
    {
        ArgumentNullException.ThrowIfNull(supportedLanguages);

        var supportedLanguageCodes = new List<LanguageCode>();
        _endonyms = new Dictionary<LanguageCode, string>();

        foreach (var language in supportedLanguages.Languages)
        {
            if (!LanguageCode.TryParseIso6391(language.IsoCode, out var languageCode))
            {
                continue;
            }

            if (_endonyms.ContainsKey(languageCode))
            {
                continue;
            }

            supportedLanguageCodes.Add(languageCode);
            _endonyms.Add(
                languageCode,
                string.IsNullOrWhiteSpace(language.DisplayName)
                    ? languageCode.Value
                    : language.DisplayName.Trim());
        }

        if (supportedLanguageCodes.Count == 0)
        {
            foreach (var isoCode in VoiceInterfaceModelCatalog.SupportedLanguages)
            {
                var languageCode = LanguageCode.ParseIso6391(isoCode);
                supportedLanguageCodes.Add(languageCode);
                _endonyms.TryAdd(languageCode, languageCode.Value);
            }
        }

        if (!LanguageCode.TryParseIso6391(supportedLanguages.DefaultIsoCode, out var defaultLanguage)
            || !supportedLanguageCodes.Contains(defaultLanguage))
        {
            var modeledDefaultIsoCode = VoiceInterfaceModelCatalog.DefaultLanguage;
            if (supportedLanguageCodes.Any(code => string.Equals(code.Value, modeledDefaultIsoCode, StringComparison.Ordinal)))
            {
                defaultLanguage = supportedLanguageCodes.First(
                    code => string.Equals(code.Value, modeledDefaultIsoCode, StringComparison.Ordinal));
            }
            else
            {
                defaultLanguage = supportedLanguageCodes[0];
            }
        }

        DefaultLanguage = defaultLanguage;
        SupportedLanguages = supportedLanguageCodes;
        _displayNames = BuildDisplayNames(supportedLanguageCodes);
    }

    public static VoiceInterfaceLanguageCatalog CreateDefault()
    {
        var supportedLanguages = VoiceInterfaceModelCatalog.SupportedLanguages
            .Select(isoCode => new SupportedLanguage(isoCode, ResolveLanguageEndonym(isoCode)))
            .ToArray();

        return new VoiceInterfaceLanguageCatalog(
            new SupportedLanguages(
                VoiceInterfaceModelCatalog.DefaultLanguage,
                supportedLanguages));
    }

    public LanguageCode DefaultLanguage { get; }

    public IReadOnlyList<LanguageCode> SupportedLanguages { get; }

    public bool IsSupported(LanguageCode language) => _endonyms.ContainsKey(language);

    public string GetEndonym(LanguageCode language)
    {
        if (_endonyms.TryGetValue(language, out var endonym))
        {
            return endonym;
        }

        throw new InvalidOperationException($"Language '{language.Value}' is not supported.");
    }

    public string GetDisplayName(LanguageCode language, LanguageCode displayLanguage)
    {
        if (_displayNames.TryGetValue((language, displayLanguage), out var displayName))
        {
            return displayName;
        }

        return GetEndonym(language);
    }

    private static Dictionary<(LanguageCode language, LanguageCode displayLanguage), string> BuildDisplayNames(
        IReadOnlyList<LanguageCode> supportedLanguages)
    {
        var result = new Dictionary<(LanguageCode language, LanguageCode displayLanguage), string>();

        foreach (var language in supportedLanguages)
        {
            var resourceId = LanguageNameResourceIdResolver.Resolve(language);

            foreach (var displayLanguage in supportedLanguages)
            {
                if (!VoiceInterfaceModelCatalog.Resources.TryGetResourceText(resourceId, displayLanguage.Value, out var text))
                {
                    continue;
                }

                result.TryAdd((language, displayLanguage), text);
            }
        }

        return result;
    }

    private static string ResolveLanguageEndonym(string isoCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        var resourceId = LanguageNameResourceIdResolver.Resolve(isoCode);
        return VoiceInterfaceModelCatalog.Resources.TryGetResourceText(resourceId, isoCode, out var text)
            ? text
            : isoCode.Trim().ToLowerInvariant();
    }
}
