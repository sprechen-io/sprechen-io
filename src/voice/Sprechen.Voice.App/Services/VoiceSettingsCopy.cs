namespace Sprechen.Voice.App.Services;

/// <summary>
/// Bündelt den Zugriff auf die Textkataloge für Einstellungen und Navigation
/// der Sprach-App.
/// </summary>
internal sealed class VoiceSettingsCopy
{
    internal VoiceSettingsCopy()
        : this(new VoiceSettingsCopyResolver())
    {
    }

    private VoiceSettingsCopy(VoiceSettingsCopyResolver resolver)
    {
        General = new VoiceSettingsGeneralCopy(resolver);
        Audio = new VoiceSettingsAudioCopy(resolver);
        SpeechCapture = new VoiceSettingsSpeechCaptureCopy(resolver);
        SessionSetup = new VoiceSettingsSessionSetupCopy(resolver);
        Politeness = new VoiceSettingsSessionSetupPolitenessCopy(resolver);
        EarconGuide = new VoiceSettingsEarconGuideCopy(resolver);
        VoiceActivation = new VoiceSettingsVoiceActivationCopy(resolver);
        SpeechReview = new VoiceSettingsSpeechReviewCopy(resolver);
        VoiceDialog = new VoiceSettingsVoiceDialogCopy(resolver);
        ApplicationOverrides = new VoiceSettingsApplicationOverridesCopy(resolver);
        Languages = new VoiceSettingsLanguageCopy(resolver);
        Theme = new VoiceSettingsThemeCopy(resolver);
        Chrome = new VoiceChromeCopy(resolver);
    }

    internal VoiceSettingsGeneralCopy General { get; }

    internal VoiceSettingsAudioCopy Audio { get; }

    internal VoiceSettingsSpeechCaptureCopy SpeechCapture { get; }

    internal VoiceSettingsSessionSetupCopy SessionSetup { get; }

    internal VoiceSettingsSessionSetupPolitenessCopy Politeness { get; }

    internal VoiceSettingsEarconGuideCopy EarconGuide { get; }

    internal VoiceSettingsVoiceActivationCopy VoiceActivation { get; }

    internal VoiceSettingsSpeechReviewCopy SpeechReview { get; }

    internal VoiceSettingsVoiceDialogCopy VoiceDialog { get; }

    internal VoiceSettingsApplicationOverridesCopy ApplicationOverrides { get; }

    internal VoiceSettingsLanguageCopy Languages { get; }

    internal VoiceSettingsThemeCopy Theme { get; }

    internal VoiceChromeCopy Chrome { get; }
}
