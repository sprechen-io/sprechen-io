namespace Sprechen.Voice.App.Services;

/// <summary>
/// Liefert allgemeine Einstellungstexte und die Anzeige der gewählten Sitzungssprache aus dem Katalog.
/// </summary>
internal sealed class VoiceSettingsGeneralCopy
{
    private readonly VoiceSettingsCopyResolver _resolver;

    internal VoiceSettingsGeneralCopy(VoiceSettingsCopyResolver resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    internal string GetSettingsTitle(string? isoCode) => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsTitle, isoCode);

    internal string GetLoadingText(string? isoCode) => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsLoading, isoCode);

    internal string GetEnabledText(string? isoCode) => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsEnabled, isoCode);

    internal string GetDisabledText(string? isoCode) => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsDisabled, isoCode);

    internal string GetSessionLanguageSummary(string? displayIsoCode, string? languageIsoCode)
    {
        return _resolver.ResolveTemplate(
            VoiceInterfaceResourceIds.SettingsSessionLanguageSummary,
            displayIsoCode,
            "{languageName}",
            _resolver.GetLanguageDisplayName(languageIsoCode, displayIsoCode));
    }

    internal string GetVoiceTitle(string? isoCode) => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsVoiceTitle, isoCode);
}
