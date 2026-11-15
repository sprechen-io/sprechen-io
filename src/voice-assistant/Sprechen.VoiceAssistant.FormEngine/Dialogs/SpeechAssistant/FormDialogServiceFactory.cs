using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Setzt die Laufzeitdienste einschließlich Zustand, Regeln und Projektionen zusammen und verbindet Wiederherstellung mit anschließender Fokusprüfung.
/// </summary>
internal static class FormDialogServiceFactory
{
    public static FormDialogDependencies Create(
        FormDialogDefinitionState definitionState,
        FormMutableStateStore mutableState,
        FormDialogServiceDependencies dependencies,
        IFormRuleLoopGuardFeedback loopGuardFeedback,
        int maxRuleIterations)
    {
        ArgumentNullException.ThrowIfNull(definitionState);
        ArgumentNullException.ThrowIfNull(mutableState);
        ArgumentNullException.ThrowIfNull(dependencies);
        ArgumentNullException.ThrowIfNull(loopGuardFeedback);

        var valueParser = FormValueParsers.Create(
            definitionState.RequestedIsoCode,
            dependencies.TranscriptParser,
            ResolveDiacriticsStrategy(definitionState.Contract));
        FormValue? GetVariableValue(string variableId)
            => TryGetValue(definitionState, mutableState, variableId, out var value) ? value : null;

        var ruleEngine = new FormRuleEngine(GetVariableValue);
        var constraintValidator = new FormConstraintValidator(mutableState.ValuesByVariableId, GetVariableValue);
        var actionExecutor = new FormActionExecutor(
            dependencies.VisibilityState,
            dependencies.FieldAccessState,
            dependencies.VariableValueState,
            dependencies.ActionNavigationState);
        var navigator = new FormNavigator(dependencies.NavigatorState);
        var jumpTargetResolver = new FormJumpTargetResolver(
            definitionState.OrderedFieldIds,
            definitionState.FieldsById,
            definitionState.OrderedFieldGroupIds,
            definitionState.Contract.Projections?.Voice?.Hints?.FieldNameSynonyms,
            definitionState.LocalizationResolver,
            dependencies.NavigatorState);
        var visibilityService = new FormFieldVisibilityPolicy(
            mutableState.FieldGroupVisibleById,
            mutableState.FieldVisibleById,
            mutableState.FieldEnabledById,
            mutableState.FieldReadOnlyById,
            definitionState.ParentFieldGroupById,
            definitionState.FieldToFieldGroupId,
            definitionState.FieldsById);
        var privacyReadbackService = new FormPrivacyReadbackPolicy(definitionState.EffectivePoliciesByVariableId);
        var inputModeAvailabilityPolicy = new FormFieldInputModeAvailabilityPolicy(
            mutableState,
            definitionState.EffectivePoliciesByVariableId);
        var focusedFieldSnapshotBuilder = new FormFocusedFieldSnapshotBuilder(
            definitionState,
            mutableState,
            inputModeAvailabilityPolicy,
            privacyReadbackService);
        var answerableFieldIds = definitionState.FieldsById
            .Where(entry => entry.Value.Editability is FieldEditability.UserEditable)
            .Select(entry => entry.Key.Value)
            .ToImmutableHashSet(StringComparer.Ordinal);
        var projectionScopeResolver = new FormDialogProjectionScopeResolver(
            definitionState.OrderedFieldGroupIds,
            definitionState.OrderedFieldIds,
            definitionState.FieldToFieldGroupId,
            answerableFieldIds);
        var discoveryProjectionService = new FormDiscoveryProjectionBuilder(
            definitionState.FieldsById,
            definitionState.FieldToFieldGroupId,
            definitionState.LocalizationResolver,
            projectionScopeResolver);
        var statusProjectionService = new FormStatusProjectionBuilder(
            definitionState.OrderedFieldIds,
            definitionState.FieldsById,
            definitionState.FieldToFieldGroupId,
            definitionState.FieldToVariableId,
            definitionState.VariablesById,
            definitionState.Constraints,
            definitionState.LocalizationResolver,
            projectionScopeResolver);
        var summaryProjectionService = new FormSummaryProjectionBuilder(
            definitionState.OrderedFieldIds
                .Where(answerableFieldIds.Contains)
                .ToImmutableArray(),
            definitionState.FieldsById,
            definitionState.FieldToVariableId,
            definitionState.VariablesById,
            definitionState.LocalizationResolver,
            definitionState.RequestedIsoCode);
        var validationContext = new FormDialogValidationContext(
            definitionState.OrderedFieldIds,
            definitionState.Constraints,
            definitionState.FieldsById,
            definitionState.FieldToVariableId,
            definitionState.FieldToFieldGroupId,
            definitionState.VariablesById,
            mutableState.ValuesByVariableId);
        var validationService = new FormDialogValidator(
            validationContext,
            ruleEngine,
            constraintValidator,
            definitionState.LocalizationResolver);
        var ruleOrchestrationService = new FormRuleOrchestrator(
            definitionState.Contract,
            ruleEngine,
            actionExecutor,
            () => FormDialogStateSnapshotStore.Take(
                mutableState.ValuesByVariableId,
                mutableState.FieldGroupVisibleById,
                mutableState.FieldVisibleById,
                mutableState.FieldEnabledById,
                mutableState.FieldReadOnlyById,
                mutableState.FieldInputModeOverrides,
                mutableState.FocusedFieldId?.Value),
            snapshot =>
            {
                // Nach der Wiederherstellung erneut prüfen, ob das Fokusziel verfügbar ist.
                mutableState.RestoreSnapshot(snapshot);
                navigator.EnsureFocusIsValid();
            },
            loopGuardFeedback,
            maxRuleIterations);
        var cqrsProjectionService = new FormCqrsProjectionAdapter(
            definitionState,
            mutableState,
            visibilityService,
            statusProjectionService,
            validationService,
            onValueChanged: () =>
            {
                // Der Regelablauf übernimmt Wiederherstellung und Fehlerrückmeldung bei fehlender Konvergenz.
                _ = ruleOrchestrationService.TryApplyRules(RuleScopes.OnValueChanged, out _);
                navigator.EnsureFocusIsValid();
            });

        return new FormDialogDependencies
        {
            ValueParser = valueParser,
            RuleEngine = ruleEngine,
            ConstraintValidator = constraintValidator,
            ActionExecutor = actionExecutor,
            Navigator = navigator,
            JumpTargetResolver = jumpTargetResolver,
            PrivacyReadbackPolicy = privacyReadbackService,
            InputModeAvailabilityPolicy = inputModeAvailabilityPolicy,

            FocusedFieldSnapshotBuilder = focusedFieldSnapshotBuilder,
            DiscoveryProjectionService = discoveryProjectionService,
            StatusProjectionService = statusProjectionService,
            SummaryProjectionService = summaryProjectionService,
            ValidationService = validationService,
            VisibilityService = visibilityService,
            RuleOrchestrationService = ruleOrchestrationService,
            CqrsProjectionService = cqrsProjectionService
        };
    }

    private static bool TryGetValue(
        FormDialogDefinitionState definitionState,
        FormMutableStateStore mutableState,
        string variableId,
        out FormValue value)
        => FormVariableValueReader.TryGetValue(definitionState.VariablesById, mutableState.ValuesByVariableId, variableId, out value);

    private static string ResolveDiacriticsStrategy(FormContract contract)
    {
        ArgumentNullException.ThrowIfNull(contract);
        return DiacriticsStrategies.NormalizeOrDefault(contract.Projections?.Voice?.FormVoiceProfile?.DiacriticsStrategy);
    }
}
