namespace Sprechen.Voice.App.Services;

/// <summary>
/// Liefert lokalisierte Texte für die sprachgeführte Überprüfung der
/// Weckwörter aus dem gemeinsamen Katalog.
/// </summary>
internal sealed class VoiceSettingsSpeechReviewCopy
{
    private readonly VoiceSettingsCopyResolver _resolver;

    internal VoiceSettingsSpeechReviewCopy(VoiceSettingsCopyResolver resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    internal string GetTitle(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsVoiceActivationSpeechReviewTitle, isoCode);

    internal string GetIntro(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsVoiceActivationSpeechReviewIntro, isoCode);

    internal string GetRun(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsVoiceActivationSpeechReviewRun, isoCode);

    internal string GetSuccess(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsVoiceActivationSpeechReviewSuccess, isoCode);
}
