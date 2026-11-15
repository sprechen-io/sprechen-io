using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.Dialogs.VoiceInterface;
using Sprechen.Speech.ModelRuntime.PromptTemplates;
using Sprechen.Speech.State;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Passt den Assistentenkontext an die gemeinsame Laufzeit für die Aktivierungsworterkennung
/// an und bereitet deren modellierte Rückmeldetexte in der Sitzungssprache vor.
/// </summary>
internal sealed class SpeechAssistantWakeWordRuntime
{
    private readonly WakeWordRuntime _runtime;

    public SpeechAssistantWakeWordRuntime(WakeWordRuntime runtime)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    }

    public async ValueTask RunAsync(
        string selectedIsoCode,
        SpeechAssistantContext context,
        IEventDispatcher<SpeechAssistantEvent> dispatcher,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selectedIsoCode);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(dispatcher);

        var outcomeSink = new SpeechAssistantWakeRuntimeOutcomeSink(dispatcher);
        var wakeRuntimeContext = CreateWakeRuntimeContext(context, selectedIsoCode);

        await _runtime
            .RunAsync(selectedIsoCode, wakeRuntimeContext, outcomeSink, cancellationToken)
            .ConfigureAwait(false);
    }

    private static WakeRuntimeContext CreateWakeRuntimeContext(SpeechAssistantContext context, string selectedIsoCode)
    {
        var wakeWordInteractionDependencies = context.InteractionDependencies.WakeWordInteractionDependencies;
        return new WakeRuntimeContext(
            new WakeWordDetectionDependencies(
                context.SetupDependencies.WakeWordProvider,
                context.SetupDependencies.AcousticWakeWordDetectorConfigurationResolver,
                wakeWordInteractionDependencies.AcousticWakeWordScorer,
                wakeWordInteractionDependencies.AcousticWakeWordTriggerPolicy),
            new WakeRuntimeCaptureDependencies(
                context.InteractionDependencies.SpeechRecognitionWorker,
                wakeWordInteractionDependencies.SegmentStreamSource),
            new WakeRuntimeFeedbackDependencies(
                context.InteractionDependencies.SpeechInteractionSignals,
                wakeWordInteractionDependencies.WakeWordTelemetrySink,
                context.InteractionDependencies.EarconPlayer,
                context.InteractionDependencies.TextToSpeechPlayer,
                TryBuildRecoveryGuidance(context, selectedIsoCode),
                BuildMicrophoneAvailabilityGuidance(
                    selectedIsoCode,
                    context.SetupDependencies.UserLanguageState.SelectedPolitenessLevel ?? PolitenessLevel.Formal),
                wakeWordInteractionDependencies.WakeMissDiagnosticsReporter));
    }

    internal static MicrophoneAvailabilityGuidance BuildMicrophoneAvailabilityGuidance(
        string selectedIsoCode,
        PolitenessLevel politenessLevel)
    {
        var policy = VoiceInterfaceModelCatalog.WakeWord.GetMicrophoneAvailabilityPolicySettings();

        var deviceNotFoundText = FormDialogFlowPromptTextResolver.GetSessionMicrophoneUnavailableDeviceNotFoundPrompt(selectedIsoCode, politenessLevel);
        var permissionDeniedText = FormDialogFlowPromptTextResolver.GetSessionMicrophoneUnavailablePermissionDeniedPrompt(selectedIsoCode, politenessLevel);
        var unavailableText = FormDialogFlowPromptTextResolver.GetSessionMicrophoneUnavailableTechnicalPrompt(selectedIsoCode, politenessLevel);
        if (!VoiceInterfaceModelCatalog.Resources.TryGetResourceText(
                VoiceInterfaceResourceIds.SpeechCaptureRecoveryMicrophoneAvailable,
                selectedIsoCode,
                out var recoveryText))
        {
            throw new ModeledVoiceResourcePromptException(
                ModeledVoiceResourcePromptException.MissingLocalizedTextCodeValue,
                $"Required microphone-recovery resource is missing for language '{selectedIsoCode}'.");
        }

        return new MicrophoneAvailabilityGuidance(
            deviceNotFoundText,
            permissionDeniedText,
            unavailableText,
            recoveryText,
            selectedIsoCode,
            policy.FailureThreshold,
            TimeSpan.FromSeconds(policy.CooldownSeconds),
            TimeSpan.FromSeconds(policy.MaxCooldownSeconds));
    }

    private static WakeRecoveryGuidance? TryBuildRecoveryGuidance(SpeechAssistantContext context, string selectedIsoCode)
    {
        var settings = VoiceInterfaceModelCatalog.WakeWord.GetRecoveryGuidanceSettings();
        if (settings is null)
        {
            return null;
        }

        // Die Hinweise mit der ausgewählten Anrede oder der formalen Standardanrede und der
        // Sprache dieses Erkennungslaufs auflösen. Haupt- und Alternativtext vorab bereitstellen,
        // damit die Rückmelderegel später ohne erneute Katalogauflösung wählen kann.
        var politenessLevel = context.SetupDependencies.UserLanguageState.SelectedPolitenessLevel ?? PolitenessLevel.Formal;
        var primaryGuidanceText = FormDialogFlowPromptTextResolver.GetSessionWakeWordRecoveryHintPrompt(selectedIsoCode, politenessLevel);
        var alternativesGuidanceText = FormDialogFlowPromptTextResolver.GetSessionWakeWordRecoveryHintAlternativesPrompt(selectedIsoCode, politenessLevel);

        return new WakeRecoveryGuidance(
            primaryGuidanceText,
            alternativesGuidanceText,
            selectedIsoCode,
            settings.MissThreshold,
            TimeSpan.FromSeconds(settings.CooldownSeconds),
            TimeSpan.FromSeconds(settings.MaxCooldownSeconds));
    }
}
