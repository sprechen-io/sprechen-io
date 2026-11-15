namespace Sprechen.Speech.App.Contracts.Dialogs.VoiceInterface;

/// <summary>
/// Benannte Textressourcen der Sprachoberfläche. Die Inventarlisten unterscheiden
/// aktiv verwendete Texte, Strukturtexte des Katalogs und zurückbehaltene Hinweistonbeschreibungen.
/// </summary>
public static class VoiceInterfaceResourceIds
{
    public const string FormSurfaceEmptyStateKicker = "formSurface.emptyState.kicker";
    public const string FormSurfaceEmptyStateTitle = "formSurface.emptyState.title";
    public const string FormSurfaceEmptyStateDescription = "formSurface.emptyState.description";
    public const string FormSurfaceLanguageSummary = "formSurface.summary.language";
    public const string FormSurfaceFocusedFieldSummary = "formSurface.summary.focusedField";
    public const string FormSurfaceFieldValueNotCaptured = "formSurface.fieldValue.notCaptured";
    public const string FormSurfaceFieldStatusListening = "formSurface.fieldStatus.listening";
    public const string FormSurfaceFieldStatusPending = "formSurface.fieldStatus.pending";
    public const string FormSurfaceFieldStatusOptional = "formSurface.fieldStatus.optional";
    public const string FormSurfaceFieldStatusNotRelevant = "formSurface.fieldStatus.notRelevant";
    public const string FormSurfaceFieldStatusCompleted = "formSurface.fieldStatus.completed";
    public const string FormSurfaceFieldStatusRequired = "formSurface.fieldStatus.required";
    public const string FormSurfaceFieldGroupStatusPending = "formSurface.fieldGroupStatus.pending";
    public const string FormSurfaceFieldGroupStatusNotRelevant = "formSurface.fieldGroupStatus.notRelevant";
    public const string FormSurfaceFieldGroupStatusCompleted = "formSurface.fieldGroupStatus.completed";
    public const string FormSurfaceFieldGroupStatusInProgress = "formSurface.fieldGroupStatus.inProgress";
    public const string FormSurfaceStageLive = "formSurface.stage.live";
    public const string FormSurfaceStageSubmitted = "formSurface.stage.submitted";
    public const string FormSurfaceStageCancelled = "formSurface.stage.cancelled";
    public const string FormSurfaceStageAbandoned = "formSurface.stage.abandoned";
    public const string SessionSetupEarconsIntro = "sessionSetup.earconsIntro";
    public const string SessionSetupEarconFlowListening = "sessionSetup.earcon.flowListening";
    public const string SessionSetupEarconFlowProcessing = "sessionSetup.earcon.flowProcessing";
    public const string SessionSetupEarconValidationSuccess = "sessionSetup.earcon.validationSuccess";
    public const string SessionSetupEarconValidationNotOk = "sessionSetup.earcon.validationNotOk";
    public const string SessionSetupEarconStructureStepTransition = "sessionSetup.earcon.structureStepTransition";
    public const string SessionSetupEarconStructureBoundary = "sessionSetup.earcon.structureBoundary";
    public const string SessionSetupEarconModePrivacyChange = "sessionSetup.earcon.modePrivacyChange";
    public const string SessionSetupEarconConfirmationRequired = "sessionSetup.earcon.confirmationRequired";
    public const string SessionSetupEarconVoiceInputMicOff = "sessionSetup.earcon.voiceInputMicOff";
    public const string SessionSetupEarconVoiceInputMicOn = "sessionSetup.earcon.voiceInputMicOn";
    public const string SessionSetupWakeWordsPrompt = "sessionSetup.wakeWords.prompt";
    public const string SessionSetupWakeWordsAskNew = "sessionSetup.wakeWords.askNew";
    public const string SessionSetupWakeWordsNotRecognized = "sessionSetup.wakeWords.notRecognized";
    public const string SessionSetupWakeWordsConfirm = "sessionSetup.wakeWords.confirm";
    public const string SessionSetupWakeWordsSaved = "sessionSetup.wakeWords.saved";
    public const string SettingsTitle = "settings.title";
    public const string SettingsLoading = "settings.loading";
    public const string SettingsEnabled = "settings.enabled";
    public const string SettingsDisabled = "settings.disabled";
    public const string SettingsSessionLanguageSummary = "settings.sessionLanguage.summary";
    public const string SettingsAudioTitle = "settings.audio.title";
    public const string SettingsAudioVolumeLabel = "settings.audio.volume.label";
    public const string SettingsAudioVolumeTest = "settings.audio.volumeTest";
    public const string SettingsAudioVolumeTestStart = "settings.audio.volumeTest.start";
    public const string SettingsAudioVolumeTestStop = "settings.audio.volumeTest.stop";
    public const string SettingsVoiceTitle = "settings.voice.title";
    public const string SettingsSpeechCaptureTitle = "settings.speechCapture.title";
    public const string SettingsSpeechCaptureSilenceDebounceLabel = "settings.speechCapture.silenceDebounce.label";
    public const string SettingsSpeechCaptureSilenceDebounceAriaLabel = "settings.speechCapture.silenceDebounce.ariaLabel";
    public const string SettingsSpeechCaptureSpeechAmplitudeThresholdLabel = "settings.speechCapture.speechAmplitudeThreshold.label";
    public const string SettingsSpeechCaptureSpeechAmplitudeThresholdAriaLabel = "settings.speechCapture.speechAmplitudeThreshold.ariaLabel";
    public const string SettingsSpeechCaptureResetButton = "settings.speechCapture.reset.button";
    public const string SettingsSpeechCaptureInvalidValue = "settings.speechCapture.invalidValue";
    public const string SpeechCaptureTranscriptSpeakingFailed = "speechCapture.transcriptSpeaking.failed";
    public const string SpeechCaptureTranscriptSpeakingConfigurationFailed = "speechCapture.transcriptSpeaking.configurationFailed";
    public const string SpeechCaptureTranscriptSpeakingLanguageUnavailable = "speechCapture.transcriptSpeaking.languageUnavailable";
    public const string SpeechCaptureTranscriptSpeakingAudioOutputUnsupported = "speechCapture.transcriptSpeaking.audioOutputUnsupported";
    public const string SpeechCaptureTranscriptSpeakingRateLimited = "speechCapture.transcriptSpeaking.rateLimited";
    public const string SpeechCaptureFailureVoiceCommandDialogFailed = "speechCapture.failure.voiceCommandDialogFailed";
    public const string SpeechCaptureFailureDefault = "speechCapture.failure.default";
    public const string SpeechCaptureFailurePermissionDenied = "speechCapture.failure.permissionDenied";
    public const string SpeechCaptureFailureMicrophoneUnavailable = "speechCapture.failure.microphoneUnavailable";
    public const string SpeechCaptureRecoveryMicrophoneAvailable = "speechCapture.recovery.microphoneAvailable";
    public const string SpeechCaptureFailureGeneric = "speechCapture.failure.generic";
    public const string SpeechCaptureFailureRuleLoopGuardExceeded = "speechCapture.failure.ruleLoopGuardExceeded";
    public const string SpeechCaptureAriaLabelListening = "speechCapture.ariaLabel.listening";
    public const string SpeechCaptureAriaLabelReady = "speechCapture.ariaLabel.ready";
    public const string SpeechCaptureAriaLabelReadyFallback = "speechCapture.ariaLabel.readyFallback";
    public const string SpeechCaptureAriaLabelStarting = "speechCapture.ariaLabel.starting";
    public const string SpeechCaptureAriaLabelPaused = "speechCapture.ariaLabel.paused";
    public const string SpeechCaptureAriaLabelFailed = "speechCapture.ariaLabel.failed";
    public const string SpeechCaptureAriaLabelDefault = "speechCapture.ariaLabel.default";
    public const string SpeechCaptureStatusListening = "speechCapture.status.listening";
    public const string SpeechCaptureStatusStarting = "speechCapture.status.starting";
    public const string SpeechCaptureStatusReady = "speechCapture.status.ready";
    public const string SpeechCaptureStatusReadyWithWakeWords = "speechCapture.status.readyWithWakeWords";
    public const string SpeechCaptureStatusPaused = "speechCapture.status.paused";
    public const string SpeechCaptureStatusFailed = "speechCapture.status.failed";
    public const string SpeechCaptureStatusDefault = "speechCapture.status.default";
    public const string SpeechCaptureLexicalFallbackOverrideIneligible = "speechCapture.lexicalFallback.overrideIneligible";
    public const string SpeechCaptureLexicalFallbackDetectorUnavailable = "speechCapture.lexicalFallback.detectorUnavailable";
    public const string SpeechCaptureLexicalFallbackDetectorWarmup = "speechCapture.lexicalFallback.detectorWarmup";
    public const string SpeechCaptureLexicalFallbackDetectorFailure = "speechCapture.lexicalFallback.detectorFailure";
    public const string SpeechCaptureLexicalFallbackRecognitionOnly = "speechCapture.lexicalFallback.recognitionOnly";
    public const string SpeechCaptureLexicalFallbackDefault = "speechCapture.lexicalFallback.default";
    public const string SettingsSessionSetupTitle = "settings.sessionSetup.title";
    public const string SettingsSessionSetupIntro = "settings.sessionSetup.intro";
    public const string SettingsSessionSetupLanguageLabel = "settings.sessionSetup.language.label";
    public const string SettingsSessionSetupLanguageHelp = "settings.sessionSetup.language.help";
    public const string SettingsSessionSetupAddressingModeTitle = "settings.sessionSetup.addressingMode.title";
    public const string SettingsSessionSetupPolitenessLabel = "settings.sessionSetup.politeness.label";
    public const string SettingsSessionSetupPolitenessFormal = "settings.sessionSetup.politeness.formal";
    public const string SettingsSessionSetupPolitenessInformal = "settings.sessionSetup.politeness.informal";
    public const string SettingsSessionSetupPolitenessHelp = "settings.sessionSetup.politeness.help";
    public const string SettingsSessionSetupNoPolitenessProfile = "settings.sessionSetup.politeness.none";
    public const string SettingsSessionSetupEffectiveTargetTitle = "settings.sessionSetup.effectiveTarget.title";
    public const string SettingsSessionSetupSavedLanguageSummary = "settings.sessionSetup.savedLanguage.summary";
    public const string SettingsSessionSetupPolitenessSummary = "settings.sessionSetup.politeness.summary";
    public const string SettingsSessionSetupPolitenessDefault = "settings.sessionSetup.politeness.default";
    public const string SettingsSessionSetupSaveButton = "settings.sessionSetup.save.button";
    public const string SettingsSessionSetupSaveSuccess = "settings.sessionSetup.save.success";
    public const string SettingsEarconGuideTitle = "settings.earconGuide.title";
    public const string SettingsEarconGuidePlayCue = "settings.earconGuide.playCue";
    public const string SettingsEarconGuideStopAll = "settings.earconGuide.stopAll";
    public const string SettingsVoiceActivationTitle = "settings.voiceActivation.title";
    public const string SettingsVoiceActivationIntro = "settings.voiceActivation.intro";
    public const string SettingsVoiceActivationModeledDefaultsTitle = "settings.voiceActivation.modeledDefaults.title";
    public const string SettingsVoiceActivationNoWakeWords = "settings.voiceActivation.modeledDefaults.none";
    public const string SettingsVoiceActivationLocalOverrideTitle = "settings.voiceActivation.localOverride.title";
    public const string SettingsVoiceActivationLocalOverrideHelp = "settings.voiceActivation.localOverride.help";
    public const string SettingsVoiceActivationSave = "settings.voiceActivation.save";
    public const string SettingsVoiceActivationReset = "settings.voiceActivation.reset";
    public const string SettingsVoiceActivationSpeechReviewTitle = "settings.voiceActivation.speechReview.title";
    public const string SettingsVoiceActivationSpeechReviewIntro = "settings.voiceActivation.speechReview.intro";
    public const string SettingsVoiceActivationSpeechReviewRun = "settings.voiceActivation.speechReview.run";
    public const string SettingsVoiceActivationSaveSuccess = "settings.voiceActivation.save.success";
    public const string SettingsVoiceActivationResetSuccess = "settings.voiceActivation.reset.success";
    public const string SettingsVoiceActivationSpeechReviewSuccess = "settings.voiceActivation.speechReview.success";
    public const string SettingsVoiceDialogTitle = "settings.voiceDialog.title";
    public const string SettingsVoiceDialogIntro = "settings.voiceDialog.intro";
    public const string SettingsVoiceDialogDeviceOverrideTitle = "settings.voiceDialog.deviceOverride.title";
    public const string SettingsVoiceDialogDeviceOverrideHelp = "settings.voiceDialog.deviceOverride.help";
    public const string SettingsVoiceDialogAutoAdvanceDeviceOverrideTitle = "settings.voiceDialog.autoAdvanceDeviceOverride.title";
    public const string SettingsVoiceDialogAutoAdvanceDeviceOverrideHelp = "settings.voiceDialog.autoAdvanceDeviceOverride.help";
    public const string SettingsVoiceDialogUseApplicationSetting = "settings.voiceDialog.deviceOverride.useApplication";
    public const string SettingsVoiceDialogAlwaysEnable = "settings.voiceDialog.deviceOverride.alwaysEnable";
    public const string SettingsVoiceDialogAlwaysDisable = "settings.voiceDialog.deviceOverride.alwaysDisable";
    public const string SettingsVoiceDialogBrowserDebugTitle = "settings.voiceDialog.browserDebug.title";
    public const string SettingsVoiceDialogBrowserDebugHelp = "settings.voiceDialog.browserDebug.help";
    public const string SettingsVoiceDialogEnableBrowserDebug = "settings.voiceDialog.browserDebug.enable";
    public const string SettingsVoiceDialogApplicationOverridesTitle = "settings.voiceDialog.applicationOverrides.title";
    public const string SettingsVoiceDialogApplicationOverridesHelp = "settings.voiceDialog.applicationOverrides.help";
    public const string SettingsVoiceDialogNoApplications = "settings.voiceDialog.applicationOverrides.none";
    public const string SettingsVoiceDialogApplicationColumn = "settings.voiceDialog.table.application";
    public const string SettingsVoiceDialogCatalogDefaultColumn = "settings.voiceDialog.table.catalogDefault";
    public const string SettingsVoiceDialogLocalOverrideColumn = "settings.voiceDialog.table.localOverride";
    public const string SettingsVoiceDialogUseCatalogDefault = "settings.voiceDialog.table.useCatalogDefault";

    /// <summary>
    /// Aktiv verwendete Textressourcen der Sprachoberfläche.
    /// Die getrennt geführten Katalog- und Kompatibilitätsressourcen sind nicht enthalten.
    /// </summary>
    public static IReadOnlyList<string> ActiveVoiceShellResourceIds =>
    [
        SessionSetupEarconsIntro,
        SessionSetupEarconFlowListening,
        SessionSetupEarconFlowProcessing,
        SessionSetupEarconValidationSuccess,
        SessionSetupEarconVoiceInputMicOff,
        SessionSetupEarconVoiceInputMicOn,
        SessionSetupWakeWordsPrompt,
        SessionSetupWakeWordsAskNew,
        SessionSetupWakeWordsNotRecognized,
        SessionSetupWakeWordsConfirm,
        SessionSetupWakeWordsSaved,
        SettingsLoading,
        SettingsSessionLanguageSummary,
        SettingsEnabled,
        SettingsDisabled,
        SettingsAudioVolumeTest
    ];

    /// <summary>
    /// Strukturtexte des Ressourcenkatalogs ohne aktiven Verbraucher im Ablauf.
    /// Sie gehören nicht zum aktiven Inventar.
    /// </summary>
    public static IReadOnlyList<string> CatalogOnlyStructureResourceIds =>
    [
        SessionSetupEarconStructureStepTransition,
        SessionSetupEarconStructureBoundary
    ];

    /// <summary>
    /// Zur Kompatibilität erhaltene Beschreibungen von Hinweistönen ohne Wiedergabe im Ablauf.
    /// Die Hinweistoneinführung lässt sie aus, damit sie keine dort unhörbaren Töne ankündigt.
    /// </summary>
    public static IReadOnlyList<string> ProducerlessEarconResourceIds =>
    [
        SessionSetupEarconValidationNotOk,
        SessionSetupEarconModePrivacyChange,
        SessionSetupEarconConfirmationRequired
    ];
}
