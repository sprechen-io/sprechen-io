using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Stellt Statusabfragen und die gemeinsame Auswahl offener Pflichtfelder bereit.
/// </summary>
internal sealed class FormStatusProjectionBuilder
{
    private readonly FormStatusSnapshotProjectionBuilder statusSnapshotProjectionBuilder;
    private readonly FormDetailedStatusSnapshotProjectionBuilder detailedStatusSnapshotProjectionBuilder;
    private readonly FormOpenRequiredFieldCounter openRequiredFieldCounter;
    private readonly FormOpenRequiredFieldSelector openRequiredFieldSelector;

    public FormStatusProjectionBuilder(
        ImmutableArray<string> orderedFieldIds,
        ImmutableDictionary<FieldId, FieldDefinition> fieldsById,
        ImmutableDictionary<FieldId, string> fieldToFieldGroupId,
        ImmutableDictionary<FieldId, string> fieldToVariableId,
        ImmutableDictionary<VariableId, VariableDefinition> variablesById,
        ImmutableArray<ConstraintDefinition> constraints,
        FormLocalizationResolver localizationResolver,
        FormDialogProjectionScopeResolver projectionScopeResolver)
    {
        _ = orderedFieldIds;
        ArgumentNullException.ThrowIfNull(localizationResolver);

        var fieldCompletionService = new FormFieldCompletionPolicy(
            fieldsById,
            fieldToVariableId,
            variablesById,
            constraints);
        var fieldGroupCompletionService = new FormFieldGroupCompletionService(
            fieldToFieldGroupId,
            constraints,
            fieldCompletionService);
        var errorViolationFilter = new FormErrorViolationFilter();

        statusSnapshotProjectionBuilder = new FormStatusSnapshotProjectionBuilder(
            projectionScopeResolver,
            fieldCompletionService,
            fieldGroupCompletionService,
            errorViolationFilter);
        detailedStatusSnapshotProjectionBuilder = new FormDetailedStatusSnapshotProjectionBuilder(
            fieldToFieldGroupId,
            projectionScopeResolver,
            fieldCompletionService,
            fieldGroupCompletionService,
            errorViolationFilter);
        openRequiredFieldSelector = new FormOpenRequiredFieldSelector(
            projectionScopeResolver,
            fieldCompletionService,
            errorViolationFilter);
        openRequiredFieldCounter = new FormOpenRequiredFieldCounter(openRequiredFieldSelector);
    }

    /// <summary>
    /// Stellt die gemeinsame Auswahl offener Pflichtfelder für Zählung und Navigation
    /// bereit, damit beide Aufrufe dieselbe Auswahlregel verwenden können.
    /// </summary>
    public FormOpenRequiredFieldSelector OpenRequiredFieldSelector => openRequiredFieldSelector;

    public FormStatusSnapshot BuildStatusSnapshot(
        FormFieldSnapshot focusedField,
        IReadOnlyDictionary<string, string> valuesByVariableId,
        ImmutableArray<FormConstraintViolation> violations,
        Func<string, bool> isFieldEffectivelyVisible)
        => statusSnapshotProjectionBuilder.BuildStatusSnapshot(
            focusedField,
            valuesByVariableId,
            violations,
            isFieldEffectivelyVisible);

    public FormDetailedStatusSnapshot BuildDetailedStatusSnapshot(
        IReadOnlyDictionary<string, string> valuesByVariableId,
        ImmutableArray<FormConstraintViolation> violations,
        Func<string, bool> isFieldEffectivelyVisible)
        => detailedStatusSnapshotProjectionBuilder.BuildDetailedStatusSnapshot(
            valuesByVariableId,
            violations,
            isFieldEffectivelyVisible);

    public int CountOpenRequiredFields(
        IReadOnlyDictionary<string, string> valuesByVariableId,
        ImmutableArray<FormConstraintViolation> violations,
        Func<string, bool> isFieldEffectivelyVisible)
        => openRequiredFieldCounter.CountOpenRequiredFields(
            valuesByVariableId,
            violations,
            isFieldEffectivelyVisible);
}
