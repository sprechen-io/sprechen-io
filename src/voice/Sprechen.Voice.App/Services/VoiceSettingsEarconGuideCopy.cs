namespace Sprechen.Voice.App.Services;

/// <summary>
/// Liefert lokalisierte Beschriftungen für die Erläuterung und Wiedergabe
/// der Earcons in den Einstellungen.
/// </summary>
internal sealed class VoiceSettingsEarconGuideCopy
{
    private readonly VoiceSettingsCopyResolver _resolver;

    internal VoiceSettingsEarconGuideCopy(VoiceSettingsCopyResolver resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    internal string GetTitle(string? isoCode) => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsEarconGuideTitle, isoCode);

    internal string GetPlayCue(string? isoCode) => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsEarconGuidePlayCue, isoCode);

    internal string GetStopAll(string? isoCode) => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsEarconGuideStopAll, isoCode);
}
