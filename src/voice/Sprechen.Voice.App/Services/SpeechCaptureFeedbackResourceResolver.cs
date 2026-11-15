namespace Sprechen.Voice.App.Services;

/// <summary>
/// Ordnet Aufnahme- und Wiedergabefehlern lokalisierte Meldungen für
/// Sprachausgabe und Anzeige zu.
/// </summary>
internal sealed class SpeechCaptureFeedbackResourceResolver
{
    private readonly VoiceSettingsCopyResolver _copyResolver;

    internal SpeechCaptureFeedbackResourceResolver()
        : this(new VoiceSettingsCopyResolver())
    {
    }

    private SpeechCaptureFeedbackResourceResolver(VoiceSettingsCopyResolver copyResolver)
    {
        _copyResolver = copyResolver;
    }

    internal string ResolveTranscriptSpeakingFailureMessage(SpeechErrorCode errorCode, string? isoCode)
    {
        return _copyResolver.ResolveText(ResolveTranscriptSpeakingFailureResourceId(errorCode), isoCode);
    }

    internal string ResolveFailureAnnouncementMessage(SpeechErrorCode errorCode, string? isoCode)
    {
        return _copyResolver.ResolveText(ResolveFailureAnnouncementResourceId(errorCode), isoCode);
    }

    internal string ResolveDisplayLanguage(string? isoCode)
    {
        return _copyResolver.ResolveDisplayLanguage(isoCode);
    }

    internal string ResolveTranscriptSpeakingFailureResourceId(SpeechErrorCode errorCode)
    {
        return errorCode switch
        {
            SpeechErrorCode.AuthMissingApiKey or
            SpeechErrorCode.AuthInvalidApiKey or
            SpeechErrorCode.VoiceConfigurationInvalidRequest or
            SpeechErrorCode.VoiceConfigurationUnsupportedVersion or
            SpeechErrorCode.VoiceConfigurationInvalidConfiguration =>
                VoiceInterfaceResourceIds.SpeechCaptureTranscriptSpeakingConfigurationFailed,
            SpeechErrorCode.LanguageCodeRequired or
            SpeechErrorCode.LanguageCodeInvalid or
            SpeechErrorCode.LanguageCodeUnsupported =>
                VoiceInterfaceResourceIds.SpeechCaptureTranscriptSpeakingLanguageUnavailable,
            SpeechErrorCode.AudioOutputMimeTypeRequired or
            SpeechErrorCode.AudioOutputMimeTypeUnsupported =>
                VoiceInterfaceResourceIds.SpeechCaptureTranscriptSpeakingAudioOutputUnsupported,
            SpeechErrorCode.ProviderRateLimited =>
                VoiceInterfaceResourceIds.SpeechCaptureTranscriptSpeakingRateLimited,
            _ => VoiceInterfaceResourceIds.SpeechCaptureTranscriptSpeakingFailed
        };
    }

    internal string ResolveFailureAnnouncementResourceId(SpeechErrorCode errorCode)
    {
        return errorCode switch
        {
            SpeechErrorCode.MicrophonePermissionDenied =>
                VoiceInterfaceResourceIds.SpeechCaptureFailurePermissionDenied,
            SpeechErrorCode.MicrophoneUnavailable =>
                VoiceInterfaceResourceIds.SpeechCaptureFailureMicrophoneUnavailable,
            SpeechErrorCode.VoiceCommandDialogFailed =>
                VoiceInterfaceResourceIds.SpeechCaptureFailureVoiceCommandDialogFailed,
            SpeechErrorCode.Unspecified =>
                VoiceInterfaceResourceIds.SpeechCaptureFailureGeneric,
            SpeechErrorCode.FormRuleLoopGuardExceeded =>
                VoiceInterfaceResourceIds.SpeechCaptureFailureRuleLoopGuardExceeded,
            _ => VoiceInterfaceResourceIds.SpeechCaptureFailureDefault
        };
    }
}
