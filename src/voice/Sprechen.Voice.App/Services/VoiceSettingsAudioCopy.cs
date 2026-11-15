namespace Sprechen.Voice.App.Services;

/// <summary>
/// Liefert lokalisierte Beschriftungen und die Testansage für die Audio-Einstellungen.
/// </summary>
internal sealed class VoiceSettingsAudioCopy
{
    private readonly VoiceSettingsCopyResolver _resolver;

    internal VoiceSettingsAudioCopy(VoiceSettingsCopyResolver resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    internal string GetTitle(string? isoCode) => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsAudioTitle, isoCode);

    internal string GetVolumeLabel(string? isoCode) => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsAudioVolumeLabel, isoCode);

    internal string GetVolumeTestStart(string? isoCode) => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsAudioVolumeTestStart, isoCode);

    internal string GetVolumeTestStop(string? isoCode) => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsAudioVolumeTestStop, isoCode);

    internal string GetVolumeTestText(string? isoCode) => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsAudioVolumeTest, isoCode);
}
