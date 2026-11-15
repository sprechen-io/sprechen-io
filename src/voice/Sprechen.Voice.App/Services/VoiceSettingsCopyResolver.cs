namespace Sprechen.Voice.App.Services;

/// <summary>
/// Löst Sprachcodes und Katalogtexte mit einer gemeinsamen Standardsprache
/// auf, damit die Einstellungstexte einheitlich ausgewählt werden.
/// </summary>
internal sealed class VoiceSettingsCopyResolver
{
    private readonly VoiceInterfaceLanguageCatalog _languageCatalog;

    internal VoiceSettingsCopyResolver()
        : this(VoiceInterfaceLanguageCatalog.CreateDefault())
    {
    }

    private VoiceSettingsCopyResolver(VoiceInterfaceLanguageCatalog languageCatalog)
    {
        _languageCatalog = languageCatalog;
    }

    internal string GetLanguageEndonym(string? isoCode)
    {
        return _languageCatalog.GetEndonym(ResolveLanguageCode(isoCode));
    }

    internal string GetLanguageDisplayName(string? languageIsoCode, string? displayIsoCode)
    {
        return _languageCatalog.ResolveDisplayName(languageIsoCode, ResolveDisplayLanguage(displayIsoCode));
    }

    internal string ResolveDisplayLanguage(string? isoCode)
    {
        return ResolveLanguageCode(isoCode).Value;
    }

    internal string ResolveTemplate(string resourceId, string? isoCode, string placeholder, string replacement)
    {
        return ResolveText(resourceId, isoCode).Replace(placeholder, replacement, StringComparison.Ordinal);
    }

    internal string ResolveText(string resourceId, string? isoCode)
    {
        var resolvedIsoCode = ResolveDisplayLanguage(isoCode);
        if (!VoiceInterfaceModelCatalog.Resources.TryGetResourceText(resourceId, resolvedIsoCode, out var text))
        {
            throw new InvalidOperationException($"Voice interface resource '{resourceId}' is missing text for '{resolvedIsoCode}'.");
        }

        return text;
    }

    private LanguageCode ResolveLanguageCode(string? isoCode)
    {
        if (!LanguageCode.TryParseIso6391(isoCode, out var language)
            || !_languageCatalog.IsSupported(language))
        {
            return _languageCatalog.DefaultLanguage;
        }

        return language;
    }
}
