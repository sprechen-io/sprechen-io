namespace Sprechen.Voice.App.Services;

/// <summary>
/// Liefert lokalisierte Beschriftungen und Hilfetexte für anwendungsspezifische
/// Einstellungen aus dem gemeinsamen Katalog.
/// </summary>
internal sealed class VoiceSettingsApplicationOverridesCopy
{
    private readonly VoiceSettingsCopyResolver _resolver;

    internal VoiceSettingsApplicationOverridesCopy(VoiceSettingsCopyResolver resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    internal string GetTitle(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsVoiceDialogApplicationOverridesTitle, isoCode);

    internal string GetHelp(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsVoiceDialogApplicationOverridesHelp, isoCode);

    internal string GetNoApplications(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsVoiceDialogNoApplications, isoCode);

    internal string GetApplicationColumn(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsVoiceDialogApplicationColumn, isoCode);

    internal string GetCatalogDefaultColumn(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsVoiceDialogCatalogDefaultColumn, isoCode);

    internal string GetLocalOverrideColumn(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsVoiceDialogLocalOverrideColumn, isoCode);

    internal string GetUseCatalogDefault(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsVoiceDialogUseCatalogDefault, isoCode);
}
