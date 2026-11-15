namespace Sprechen.Voice.App.Services;

/// <summary>
/// Liefert lokalisierte Texte für Dialog- und Diagnoseeinstellungen
/// aus dem gemeinsamen Katalog.
/// </summary>
internal sealed class VoiceSettingsVoiceDialogCopy
{
    private readonly VoiceSettingsCopyResolver _resolver;

    internal VoiceSettingsVoiceDialogCopy(VoiceSettingsCopyResolver resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    internal string GetTitle(string? isoCode) => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsVoiceDialogTitle, isoCode);

    internal string GetIntro(string? isoCode) => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsVoiceDialogIntro, isoCode);

    internal string GetDeviceOverrideTitle(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsVoiceDialogDeviceOverrideTitle, isoCode);

    internal string GetDeviceOverrideHelp(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsVoiceDialogDeviceOverrideHelp, isoCode);

    internal string GetAutoAdvanceDeviceOverrideTitle(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsVoiceDialogAutoAdvanceDeviceOverrideTitle, isoCode);

    internal string GetAutoAdvanceDeviceOverrideHelp(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsVoiceDialogAutoAdvanceDeviceOverrideHelp, isoCode);

    internal string GetUseApplicationSetting(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsVoiceDialogUseApplicationSetting, isoCode);

    internal string GetAlwaysEnable(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsVoiceDialogAlwaysEnable, isoCode);

    internal string GetAlwaysDisable(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsVoiceDialogAlwaysDisable, isoCode);

    internal string GetBrowserDebugTitle(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsVoiceDialogBrowserDebugTitle, isoCode);

    internal string GetBrowserDebugHelp(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsVoiceDialogBrowserDebugHelp, isoCode);

    internal string GetEnableBrowserDebug(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsVoiceDialogEnableBrowserDebug, isoCode);
}
