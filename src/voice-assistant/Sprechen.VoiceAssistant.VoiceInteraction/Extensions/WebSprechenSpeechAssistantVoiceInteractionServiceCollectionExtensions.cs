using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Extensions;

/// <summary>
/// Registriert die Formular-Sprachinteraktionsdienste und ihre vorgesehenen
/// Lebensdauern im DI-Container, damit der Formularlauf deren gemeinsame Verträge auflösen kann.
/// </summary>
public static class WebSprechenSpeechAssistantVoiceInteractionServiceCollectionExtensions
{
    public static IServiceCollection AddWebSprechenSpeechAssistantVoiceInteraction(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<FormVoiceCommandCatalogSnapshotLoader>();
        services.AddScoped<FormVoiceCommandContractLoader>();
        services.AddScoped<FormVoiceCommandSessionResultFactory>();
        services.AddScoped<FormVoiceCommandSessionSurfacePublisher>();
        services.AddSingleton<FormVoiceCommandTerminalWorkLifetime>();
        services.AddScoped<FormVoiceCommandTerminalFeedbackOwner>();
        services.AddScoped<FormVoiceCommandFieldCoordinator>();
        services.AddScoped<DigitsWorkflowLocalCommandMapper>();
        services.AddScoped<DigitsWorkflowGlobalCommandMapper>();
        services.AddScoped<DigitsWorkflowCommandResolver>();
        services.AddScoped<SpellingWorkflowLocalCommandMapper>();
        services.AddScoped<SpellingWorkflowGlobalCommandMapper>();
        services.AddScoped<SpellingWorkflowCommandResolver>();
        services.AddScoped<SetWorkflowGlobalCommandMapper>();
        services.AddScoped<SetWorkflowCommandResolver>();
        services.AddScoped<SharedVariableTargetVariableSet>();
        services.AddScoped<SharedVariableSourceFormOrder>();
        services.AddScoped<SharedVariableContractUsageResolver>();
        services.AddScoped<SharedVariableAdoptionSourceResolver>();
        services.AddScoped<SharedVariableAdoptionFlow>();
        services.AddScoped<SharedVariableResolver>();
        services.AddScoped<SharedVariableAnnouncementLabelResolver>();
        services.AddScoped<SharedVariableAnnouncementSourceResolver>();
        services.AddScoped<SharedVariableAnnouncementContentResolver>();
        services.AddScoped<SharedVariableAnnouncementPromptResolver>();
        services.AddScoped<IFormVoiceCommandContextLoader, FormVoiceCommandContextLoader>();
        services.AddScoped<IFormVoiceCommandSessionRunner, FormVoiceCommandSessionRunner>();
        services.AddScoped<IFormVoiceCommandSharedVariableAnnouncementService, FormVoiceCommandSharedVariableAnnouncementService>();
        services.AddScoped<FormVoiceCommandFocusedFieldHelpSpeaker>();
        services.AddScoped<FormVoiceCommandPromptTextResolver>();
        services.AddScoped<FormVoiceCommandPromptIdSpeaker>();
        services.AddScoped<FormVoiceCommandFormOrientationSpeaker>();
        services.AddScoped<IFormVoiceCommandPromptService, FormVoiceCommandPromptService>();
        services.AddScoped<IFormVoiceCommandSubmissionService, FormVoiceCommandSubmissionService>();
        services.TryAddScoped<LanguageChangeTargetResolver>();
        services.AddScoped<FormVoiceCommandPolitenessResolver>();
        services.AddSingleton<FormVoiceCommandFormDisplayNameResolver>();
        services.AddSingleton<FormVoiceCommandNumericFormArgumentMatcher>();
        services.AddSingleton<FormVoiceCommandNamedFormArgumentMatcher>();
        services.AddSingleton<IFormVoiceCommandFormMatcher, FormVoiceCommandFormMatcher>();
        services.AddScoped<FormVoiceCommandSwitchFormNavigationExit>();
        services.AddScoped<FormVoiceCommandSwitchFormTargetResolver>();
        services.AddScoped<FormVoiceCommandSwitchFormPrerequisiteResolver>();
        services.AddScoped<FormVoiceCommandSwitchFormPrerequisiteGuard>();
        services.AddScoped<FormVoiceCommandSwitchFormConfirmationFlow>();
        services.AddScoped<FormVoiceCommandGlobalCommandExecutionService>();
        services.AddScoped<FormVoiceCommandFieldErrorHandlingService>();
        services.AddScoped<CharacterAccumulationWorkflowDriver>();
        services.AddScoped<WorkflowUnrecognizedInputAnnouncer>();
        services.AddScoped<DigitsWorkflowTranscriptCapture>();
        services.AddScoped<DigitsWorkflowNoValuePrompt>();
        services.AddScoped<DigitsWorkflowCommandActionFlow>();
        services.AddScoped<DigitsWorkflowInputRejectionFlow>();
        services.AddScoped<FormVoiceCommandDigitsWorkflowService>();
        services.AddScoped<SpellingWorkflowNoValuePrompt>();
        services.AddScoped<SpellingWorkflowCommandActionFlow>();
        services.AddScoped<SpellingWorkflowInputRejectionFlow>();
        services.AddScoped<FormVoiceCommandSpellingWorkflowService>();
        services.AddScoped<CharacterAccumulationAnchoredCommandFlow>();
        services.AddScoped<SetWorkflowCommandActionFlow>();
        services.AddScoped<SetWorkflowInputRejectionFlow>();
        services.AddScoped<FormVoiceCommandSetWorkflowService>();
        services.AddScoped<DateComponentEntryWorkflowDriver>();
        services.AddScoped<FormVoiceCommandAmbiguitySignalEligibility>();
        services.AddScoped<FormVoiceCommandAmbiguityLabelResolver>();
        services.AddScoped<FormVoiceCommandAmbiguousSignalDetector>();
        services.AddScoped<FormVoiceCommandAmbiguityResolver>();
        services.AddScoped<FormVoiceCommandFieldTranscriptCapture>();
        services.AddScoped<InputVsCommandResolutionStrategyMapper>();
        services.AddScoped<FormVoiceCommandInputVsCommandTelemetry>();
        services.AddScoped<FormVoiceCommandParsedCommandFailureDecisionResolver>();
        services.AddScoped<FormVoiceCommandParsedCommandExecutionDecisionResolver>();
        services.AddScoped<ParsedCommandBranchDecisionResolver>();
        services.AddScoped<ParsedCommandBranch>();
        services.AddScoped<FormVoiceCommandInputAvailabilityGuard>();
        services.AddScoped<FormVoiceCommandDateComponentTranscriptBranch>();
        services.AddScoped<FormVoiceCommandDigitsTranscriptBranch>();
        services.AddScoped<FormVoiceCommandSpellingTranscriptBranch>();
        services.AddScoped<FormVoiceCommandSetTranscriptBranch>();
        services.AddScoped<FormVoiceCommandAmbiguityTranscriptBranch>();
        services.AddScoped<FormVoiceCommandTranscriptSourceResolver>();
        services.AddScoped<FormVoiceCommandParsedCommandResolver>();
        services.AddScoped<FormVoiceCommandInterpretationModeResolver>();
        services.AddScoped<FormVoiceCommandTranscriptInterpretationService>();
        services.AddScoped<FormVoiceCommandFieldTranscriptOrchestrator>();
        services.AddScoped<FormVoiceCommandFieldEntryDependencies>();
        services.AddScoped<FormVoiceCommandReadOnlyFieldSpeaker>();
        services.AddScoped<FormVoiceCommandConfirmationDecisionService>();
        services.AddScoped<FormVoiceCommandConfirmationValuePresenter>();
        services.AddScoped<FormVoiceCommandConfirmationRepairActionFlow>();
        services.AddScoped<FormClosingReviewTurnFlow>();
        services.AddScoped<FormVoiceCommandConfirmedFieldCompletionFlow>();
        services.AddScoped<FormVoiceCommandConfirmationTranscriptCapture>();
        services.AddScoped<FormVoiceCommandConfirmationMissingDecisionPolicy>();
        services.AddScoped<FormVoiceCommandConfirmationCommandDecisionMapper>();
        services.AddScoped<FormVoiceCommandConfirmationTranscriptInterpreter>();
        services.AddScoped<FormVoiceCommandConfirmationHandler>();
        services.AddScoped<FormVoiceCommandAdditionalFieldProposalHandler>();
        services.AddScoped<SpellingConfidenceEscalationPolicy>();

        services.AddScoped<CaptureReplayPolicy>();
        services.AddScoped<FormVoiceCommandSpellingEscalationFlow>();
        services.AddScoped<FormVoiceCommandFieldApplyService>();
        services.AddScoped<IFormVoiceCommandFieldInteractionHandler, FormVoiceCommandFieldInteractionHandler>();
        services.AddScoped<IFormVoiceCommandGlobalCommandHandler, FormVoiceCommandErrorHandler>();
        services.AddScoped<IFormVoiceCommandGlobalCommandHandler, FormVoiceCommandNextOpenRequiredFieldHandler>();
        services.AddScoped<IFormVoiceCommandGlobalCommandHandler, FormVoiceCommandCommandListHandler>();
        services.AddScoped<IFormVoiceCommandGlobalCommandHandler, FormVoiceCommandCancelHandler>();
        services.AddScoped<IFormVoiceCommandGlobalCommandHandler, FormVoiceCommandChangeLanguageHandler>();
        services.AddScoped<IFormVoiceCommandGlobalCommandHandler, FormVoiceCommandChangePolitenessHandler>();
        services.AddScoped<IFormVoiceCommandGlobalCommandHandler, FormVoiceCommandAllowBargeInHandler>();
        services.AddScoped<IFormVoiceCommandGlobalCommandHandler, FormVoiceCommandSwitchFormHandler>();
        services.AddScoped<IVoiceCommandDialog, FormVoiceCommandDialog>();
        services.AddScoped<IFormSilentInputTransaction, FormVoiceCommandSilentInputTransaction>();

        return services;
    }
}
