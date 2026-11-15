using Sprechen.Common.Cqrs;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Localization;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormEditing;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormFocus;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormPrivacy;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormRules;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormStatus;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Baut Definition, Zustandsspeicher, Dienste, Navigation und Befehlsausführer eines
/// Formularlaufs zusammen. Zuerst werden bekannte Anfangswerte, danach fehlende modellierte
/// Werte eingetragen. Fokusinitialisierung und Anfangsregeln folgen beim Laufzeitbesitzer.
/// </summary>
internal static class FormRuntimeCompositionFactory
{
    public static FormRuntimeComposition Create(
        FormContract contract,
        string requestedIsoCode,
        PolitenessLevel politenessLevel,
        IFormTranscriptParser transcriptParser,
        IReadOnlyDictionary<string, string> initialValuesByVariableId,
        IFormRuleLoopGuardFeedback loopGuardFeedback,
        int maxRuleIterations)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(transcriptParser);
        ArgumentNullException.ThrowIfNull(initialValuesByVariableId);
        ArgumentNullException.ThrowIfNull(loopGuardFeedback);

        var definitionState = FormDialogDefinitionStateFactory.Create(contract, requestedIsoCode, politenessLevel);
        var mutableState = new FormMutableStateStore(
            definitionState.VariablesById.Keys.ToHashSet(),
            definitionState.FieldsById.Keys.ToHashSet(),
            definitionState.FieldGroupsById.Keys.ToHashSet());
        var navigationState = new FormDialogNavigationState(definitionState, mutableState);
        var services = FormDialogServiceFactory.Create(
            definitionState,
            mutableState,
            new FormDialogServiceDependencies(
                transcriptParser,
                mutableState,
                mutableState,
                mutableState,
                navigationState,
                navigationState),
            loopGuardFeedback,
            maxRuleIterations);
        navigationState.Bind(services);
        var transcriptApplicator = new FormTranscriptApplicator(definitionState, mutableState, services);
        var multiSlotExtractor = new MultiSlotValueExtractor(
            services.JumpTargetResolver,
            services.ValueParser,
            services.InputModeAvailabilityPolicy,
            definitionState.FieldsById,
            definitionState.FieldToVariableId,
            definitionState.VariablesById,
            definitionState.LocalizationResolver);
        var additionalFieldProposal = new FormAdditionalFieldProposalDependencies(
            multiSlotExtractor,
            new FormAdditionalFieldProposalPolicy(
                definitionState,
                mutableState,
                services,
                new FormAdditionalFieldProposalTransactionFactory(
                    definitionState,
                    mutableState,
                    transcriptParser,
                    loopGuardFeedback,
                    maxRuleIterations)));
        var validationErrorFocus = new FormValidationErrorFocusPolicy(
            definitionState.OrderedFieldIds,
            definitionState.FieldIndexById);
        var openRequiredFieldFocus = new FormOpenRequiredFieldFocusPolicy(
            definitionState.OrderedFieldIds,
            definitionState.FieldIndexById);

        var stateAdapter = new FormDialogCqrsStateAdapter(services.CqrsProjectionService);
        var eventStore = new InMemoryEventStore();
        var navigationExecutor = new CqrsFormNavigationExecutor(
            stateAdapter,
            stateAdapter,
            eventStore,
            services.JumpTargetResolver,
            validationErrorFocus,
            new FormOpenRequiredFieldNavigationDependencies(
                services.StatusProjectionService.OpenRequiredFieldSelector,
                openRequiredFieldFocus),
            new FormFocusCommandDependencies(
                services.ValidationService,
                services.VisibilityService,
                services.Navigator,
                mutableState));
        var rulesExecutor = new CqrsFormRulesExecutor(services.RuleOrchestrationService, services.Navigator);
        var editingExecutor = new CqrsFormEditingExecutor(transcriptApplicator);
        var focusExecutor = new CqrsFormFocusExecutor(
            services.FocusedFieldSnapshotBuilder,
            services.Navigator,
            mutableState,
            definitionState,
            services.InputModeAvailabilityPolicy);
        var privacyExecutor = new CqrsFormPrivacyExecutor(services.PrivacyReadbackPolicy);
        var statusExecutor = new CqrsFormStatusExecutor(
            mutableState,
            services.StatusProjectionService,
            services.SummaryProjectionService,
            services.DiscoveryProjectionService,
            services.FocusedFieldSnapshotBuilder,
            services.ValidationService,
            services.VisibilityService,
            services.PrivacyReadbackPolicy);

        new FormInitialRuntimeValueLoader().ApplyKnownValues(
            definitionState.VariablesById,
            mutableState,
            initialValuesByVariableId);
        FormInitialValueApplier.ApplyMissingInitialValues(definitionState.VariablesById, mutableState);

        return new FormRuntimeComposition(
            mutableState,
            definitionState,
            services,
            transcriptApplicator,
            validationErrorFocus,
            openRequiredFieldFocus,
            additionalFieldProposal,
            navigationExecutor,
            rulesExecutor,
            editingExecutor,
            focusExecutor,
            privacyExecutor,
            statusExecutor,
            eventStore);
    }
}
