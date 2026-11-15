namespace Sprechen.Voice.App.Services;

/// <summary>
/// Liefert lokalisierte Texte für Sprachwahl, Anrede und Speicherung der Sitzungseinstellungen.
/// </summary>
internal sealed class VoiceSettingsSessionSetupCopy
{
    private readonly VoiceSettingsCopyResolver _resolver;

    internal VoiceSettingsSessionSetupCopy(VoiceSettingsCopyResolver resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    internal string GetTitle(string? isoCode) => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsSessionSetupTitle, isoCode);

    internal string GetIntro(string? isoCode) => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsSessionSetupIntro, isoCode);

    internal string GetLanguageLabel(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsSessionSetupLanguageLabel, isoCode);

    internal string GetLanguageHelp(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsSessionSetupLanguageHelp, isoCode);

    internal string GetAddressingModeTitle(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsSessionSetupAddressingModeTitle, isoCode);

    internal string GetEffectiveTargetTitle(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsSessionSetupEffectiveTargetTitle, isoCode);

    internal string GetSavedLanguageSummary(string? displayIsoCode, string? languageIsoCode)
    {
        return _resolver.ResolveTemplate(
            VoiceInterfaceResourceIds.SettingsSessionSetupSavedLanguageSummary,
            displayIsoCode,
            "{languageName}",
            _resolver.GetLanguageDisplayName(languageIsoCode, displayIsoCode));
    }

    internal string GetSaveButton(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsSessionSetupSaveButton, isoCode);

    internal string GetSaveSuccess(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsSessionSetupSaveSuccess, isoCode);
}
