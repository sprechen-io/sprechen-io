namespace Sprechen.Voice.App.Services;

/// <summary>
/// Liefert Sprachnamen aus dem Katalog und löst die Anzeigesprache auf,
/// damit die Sprachwahl einheitliche Bezeichnungen verwendet.
/// </summary>
internal sealed class VoiceSettingsLanguageCopy
{
    private readonly VoiceSettingsCopyResolver _resolver;

    internal VoiceSettingsLanguageCopy(VoiceSettingsCopyResolver resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    internal string GetEndonym(string? isoCode)
    {
        return _resolver.GetLanguageEndonym(isoCode);
    }

    internal string GetDisplayName(string? languageIsoCode, string? displayIsoCode)
    {
        return _resolver.GetLanguageDisplayName(languageIsoCode, displayIsoCode);
    }

    internal string ResolveDisplayLanguage(string? isoCode)
    {
        return _resolver.ResolveDisplayLanguage(isoCode);
    }
}
