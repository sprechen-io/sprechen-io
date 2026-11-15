namespace Sprechen.Voice.App.Services;

/// <summary>
/// Liefert lokalisierte Beschriftungen und Fehlermeldungen für die Einstellungen der Spracherfassung.
/// </summary>
internal sealed class VoiceSettingsSpeechCaptureCopy
{
    private readonly VoiceSettingsCopyResolver _resolver;

    internal VoiceSettingsSpeechCaptureCopy(VoiceSettingsCopyResolver resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    internal string GetTitle(string? isoCode) => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsSpeechCaptureTitle, isoCode);

    internal string GetSilenceDebounceLabel(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsSpeechCaptureSilenceDebounceLabel, isoCode);

    internal string GetSilenceDebounceAriaLabel(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsSpeechCaptureSilenceDebounceAriaLabel, isoCode);

    internal string GetSpeechAmplitudeThresholdLabel(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsSpeechCaptureSpeechAmplitudeThresholdLabel, isoCode);

    internal string GetSpeechAmplitudeThresholdAriaLabel(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsSpeechCaptureSpeechAmplitudeThresholdAriaLabel, isoCode);

    internal string GetResetButton(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsSpeechCaptureResetButton, isoCode);

    internal string GetInvalidValue(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsSpeechCaptureInvalidValue, isoCode);
}
