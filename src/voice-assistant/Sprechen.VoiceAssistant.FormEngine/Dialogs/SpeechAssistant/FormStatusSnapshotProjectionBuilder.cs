using System.Collections.Immutable;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Erstellt den kompakten Status aus Feldern des ermittelten Bereichs und Validierungsverstößen.
/// Die Anzahl offener Pflichtfelder zählt eindeutige Feldkennungen mit Pflichtverstößen vom Schweregrad Error.
/// </summary>
internal sealed class FormStatusSnapshotProjectionBuilder
{
    private readonly FormDialogProjectionScopeResolver projectionScopeResolver;
    private readonly FormFieldCompletionPolicy fieldCompletionService;
    private readonly FormFieldGroupCompletionService fieldGroupCompletionService;
    private readonly FormErrorViolationFilter errorViolationFilter;

    public FormStatusSnapshotProjectionBuilder(
        FormDialogProjectionScopeResolver projectionScopeResolver,
        FormFieldCompletionPolicy fieldCompletionService,
        FormFieldGroupCompletionService fieldGroupCompletionService,
        FormErrorViolationFilter errorViolationFilter)
    {
        this.projectionScopeResolver = projectionScopeResolver ?? throw new ArgumentNullException(nameof(projectionScopeResolver));
        this.fieldCompletionService = fieldCompletionService ?? throw new ArgumentNullException(nameof(fieldCompletionService));
        this.fieldGroupCompletionService = fieldGroupCompletionService ?? throw new ArgumentNullException(nameof(fieldGroupCompletionService));
        this.errorViolationFilter = errorViolationFilter ?? throw new ArgumentNullException(nameof(errorViolationFilter));
    }

    public FormStatusSnapshot BuildStatusSnapshot(
        FormFieldSnapshot focusedField,
        IReadOnlyDictionary<string, string> valuesByVariableId,
        ImmutableArray<FormConstraintViolation> violations,
        Func<string, bool> isFieldEffectivelyVisible)
    {
        ArgumentNullException.ThrowIfNull(focusedField);
        ArgumentNullException.ThrowIfNull(valuesByVariableId);
        ArgumentNullException.ThrowIfNull(isFieldEffectivelyVisible);

        var scope = projectionScopeResolver.Resolve(isFieldEffectivelyVisible, focusedField.FieldGroupId);
        var visibleFieldIds = scope.AnswerableFieldIds;
        var fieldIndex = ResolveFieldIndex(visibleFieldIds, focusedField.FieldId);
        var requiredFieldIds = visibleFieldIds
            .Where(fieldCompletionService.IsRequiredField)
            .ToImmutableArray();
        var fieldGroupIds = scope.RelevantFieldGroupIds;
        var fieldGroupIndex = ResolveFieldGroupIndex(fieldGroupIds, focusedField.FieldGroupId);
        var errorViolations = errorViolationFilter.Filter(violations);

        var blockingFieldIds = BuildBlockingFieldIds(errorViolations);
        var blockingFieldGroupIds = fieldGroupCompletionService.BuildScopedBlockingFieldGroupIds(errorViolations, fieldGroupIds);

        return new FormStatusSnapshot(
            Navigation: new FormStatusNavigationPosition(
                CurrentFormId: null,
                CurrentFormIndex: null,
                TotalForms: null,
                CurrentFieldGroupId: focusedField.FieldGroupId,
                CurrentFieldGroupIndex: fieldGroupIndex + 1,
                TotalFieldGroups: fieldGroupIds.Length,
                CurrentFieldId: focusedField.FieldId),
            Progress: new FormStatusCompletionProgress(
                AllFields: new FormStatusProgressDimension(CountCompletedFields(visibleFieldIds, blockingFieldIds, valuesByVariableId), visibleFieldIds.Length),
                RequiredFieldsOnly: new FormStatusProgressDimension(CountCompletedFields(requiredFieldIds, blockingFieldIds, valuesByVariableId), requiredFieldIds.Length),
                FieldGroupBased: new FormStatusProgressDimension(
                    fieldGroupCompletionService.CountCompletedFieldGroups(fieldGroupIds, requiredFieldIds, blockingFieldIds, blockingFieldGroupIds, valuesByVariableId),
                    fieldGroupIds.Length)),
            CurrentFieldLabel: focusedField.Label,
            CurrentFieldIndex: fieldIndex,
            CurrentFieldGroupTitle: focusedField.FieldGroupTitle,
            OpenRequiredFieldCount: CountOpenRequiredErrors(errorViolations),
            ValidationErrorCount: errorViolations.Length);
    }

    private static int ResolveFieldIndex(ImmutableArray<string> visibleFieldIds, string fieldId)
    {
        var visibleIndexByFieldId = visibleFieldIds
            .Select((currentFieldId, index) => (currentFieldId, index))
            .ToImmutableDictionary(pair => pair.currentFieldId, pair => pair.index, StringComparer.Ordinal);

        return visibleIndexByFieldId.TryGetValue(fieldId, out var index) ? index + 1 : 1;
    }

    private static int ResolveFieldGroupIndex(ImmutableArray<string> fieldGroupIds, string fieldGroupId)
    {
        var fieldGroupIndex = fieldGroupIds.IndexOf(fieldGroupId);
        return fieldGroupIndex < 0 ? 0 : fieldGroupIndex;
    }

    private static ImmutableHashSet<string> BuildBlockingFieldIds(ImmutableArray<FormConstraintViolation> errorViolations)
        => errorViolations
            .Select(violation => violation.FieldId)
            .Where(fieldId => !string.IsNullOrWhiteSpace(fieldId))
            .ToImmutableHashSet(StringComparer.Ordinal);

    private int CountCompletedFields(
        ImmutableArray<string> fieldIds,
        ImmutableHashSet<string> blockingFieldIds,
        IReadOnlyDictionary<string, string> valuesByVariableId)
        => fieldIds.Count(fieldId => fieldCompletionService.IsFieldCompleted(fieldId, blockingFieldIds, valuesByVariableId));

    private int CountOpenRequiredErrors(ImmutableArray<FormConstraintViolation> errorViolations)
        => errorViolations
            .Where(fieldCompletionService.IsRequiredViolation)
            .Select(violation => violation.FieldId)
            .Where(fieldId => !string.IsNullOrWhiteSpace(fieldId))
            .ToImmutableHashSet(StringComparer.Ordinal)
            .Count;
}
