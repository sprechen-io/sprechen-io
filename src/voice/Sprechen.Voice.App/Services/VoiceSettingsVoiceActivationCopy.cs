namespace Sprechen.Voice.App.Services;

/// <summary>
/// Liefert lokalisierte Texte zur Einrichtung der Weckwörter
/// aus dem gemeinsamen Katalog.
/// </summary>
internal sealed class VoiceSettingsVoiceActivationCopy
{
    private readonly VoiceSettingsCopyResolver _resolver;

    internal VoiceSettingsVoiceActivationCopy(VoiceSettingsCopyResolver resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    internal string GetTitle(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsVoiceActivationTitle, isoCode);

    internal string GetIntro(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsVoiceActivationIntro, isoCode);

    internal string GetModeledDefaultsTitle(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsVoiceActivationModeledDefaultsTitle, isoCode);

    internal string GetNoWakeWords(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsVoiceActivationNoWakeWords, isoCode);

    internal string GetLocalOverrideTitle(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsVoiceActivationLocalOverrideTitle, isoCode);

    internal string GetLocalOverrideHelp(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsVoiceActivationLocalOverrideHelp, isoCode);

    internal string GetSave(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsVoiceActivationSave, isoCode);

    internal string GetReset(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsVoiceActivationReset, isoCode);

    internal string GetSaveSuccess(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsVoiceActivationSaveSuccess, isoCode);

    internal string GetResetSuccess(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsVoiceActivationResetSuccess, isoCode);
}
