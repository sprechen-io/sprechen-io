using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Erstellt den ausführlichen Status der ausgewählten Felder und relevanter Gruppen aus Werten und Validierungsfehlern. Für die Auswahl gelten gemeinsame Regeln.
/// </summary>
internal sealed class FormDetailedStatusSnapshotProjectionBuilder
{
    private readonly ImmutableDictionary<FieldId, string> fieldToFieldGroupId;
    private readonly FormDialogProjectionScopeResolver projectionScopeResolver;
    private readonly FormFieldCompletionPolicy fieldCompletionService;
    private readonly FormFieldGroupCompletionService fieldGroupCompletionService;
    private readonly FormErrorViolationFilter errorViolationFilter;

    public FormDetailedStatusSnapshotProjectionBuilder(
        ImmutableDictionary<FieldId, string> fieldToFieldGroupId,
        FormDialogProjectionScopeResolver projectionScopeResolver,
        FormFieldCompletionPolicy fieldCompletionService,
        FormFieldGroupCompletionService fieldGroupCompletionService,
        FormErrorViolationFilter errorViolationFilter)
    {
        this.fieldToFieldGroupId = fieldToFieldGroupId ?? throw new ArgumentNullException(nameof(fieldToFieldGroupId));
        this.projectionScopeResolver = projectionScopeResolver ?? throw new ArgumentNullException(nameof(projectionScopeResolver));
        this.fieldCompletionService = fieldCompletionService ?? throw new ArgumentNullException(nameof(fieldCompletionService));
        this.fieldGroupCompletionService = fieldGroupCompletionService ?? throw new ArgumentNullException(nameof(fieldGroupCompletionService));
        this.errorViolationFilter = errorViolationFilter ?? throw new ArgumentNullException(nameof(errorViolationFilter));
    }

    public FormDetailedStatusSnapshot BuildDetailedStatusSnapshot(
        IReadOnlyDictionary<string, string> valuesByVariableId,
        ImmutableArray<FormConstraintViolation> violations,
        Func<string, bool> isFieldEffectivelyVisible)
    {
        ArgumentNullException.ThrowIfNull(valuesByVariableId);
        ArgumentNullException.ThrowIfNull(isFieldEffectivelyVisible);

        var scope = projectionScopeResolver.Resolve(isFieldEffectivelyVisible, fallbackFieldGroupId: string.Empty);
        var visibleFieldIds = scope.VisibleFieldIds;
        var requiredFieldIds = visibleFieldIds
            .Where(fieldCompletionService.IsRequiredField)
            .ToImmutableArray();
        var requiredFieldIdsSet = requiredFieldIds.ToImmutableHashSet(StringComparer.Ordinal);
        var fieldGroupIds = scope.RelevantFieldGroupIds;
        var errorViolations = errorViolationFilter.Filter(violations);

        var blockingFieldIds = BuildBlockingFieldIds(errorViolations);
        var blockingFieldGroupIds = fieldGroupCompletionService.BuildScopedBlockingFieldGroupIds(errorViolations, fieldGroupIds);
        var effectiveBlockingFieldGroupIds = fieldGroupCompletionService.BuildEffectiveBlockingFieldGroupIds(blockingFieldIds, blockingFieldGroupIds);
        var requiredFieldIdsByFieldGroupId = fieldGroupCompletionService.GroupRequiredFieldIdsByFieldGroup(requiredFieldIds);

        return new FormDetailedStatusSnapshot(
            BuildFieldSnapshots(visibleFieldIds, fallbackFieldGroupId: string.Empty, requiredFieldIdsSet, blockingFieldIds, errorViolations, valuesByVariableId),
            BuildFieldGroupSnapshots(fieldGroupIds, requiredFieldIdsByFieldGroupId, blockingFieldIds, effectiveBlockingFieldGroupIds, errorViolations, valuesByVariableId));
    }

    private ImmutableArray<FormFieldStatusSnapshot> BuildFieldSnapshots(
        ImmutableArray<string> visibleFieldIds,
        string fallbackFieldGroupId,
        ImmutableHashSet<string> requiredFieldIdsSet,
        ImmutableHashSet<string> blockingFieldIds,
        ImmutableArray<FormConstraintViolation> errorViolations,
        IReadOnlyDictionary<string, string> valuesByVariableId)
    {
        var fieldSnapshots = new List<FormFieldStatusSnapshot>(visibleFieldIds.Length);
        foreach (var fieldId in visibleFieldIds)
        {
            var fieldGroupId = fieldToFieldGroupId.TryGetValue(FieldId.From(fieldId), out var resolvedFieldGroupId)
                ? resolvedFieldGroupId
                : fallbackFieldGroupId;

            fieldSnapshots.Add(new FormFieldStatusSnapshot(
                fieldId,
                fieldGroupId,
                Relevant: true,
                Required: requiredFieldIdsSet.Contains(fieldId),
                Completed: fieldCompletionService.IsFieldCompleted(fieldId, blockingFieldIds, valuesByVariableId),
                BlockingErrorCodes: BuildBlockingFieldErrorCodes(errorViolations, fieldId)));
        }

        return fieldSnapshots.ToImmutableArray();
    }

    private ImmutableArray<FormFieldGroupStatusSnapshot> BuildFieldGroupSnapshots(
        ImmutableArray<string> fieldGroupIds,
        IReadOnlyDictionary<string, ImmutableArray<string>> requiredFieldIdsByFieldGroupId,
        ImmutableHashSet<string> blockingFieldIds,
        ImmutableHashSet<string> effectiveBlockingFieldGroupIds,
        ImmutableArray<FormConstraintViolation> errorViolations,
        IReadOnlyDictionary<string, string> valuesByVariableId)
    {
        var fieldGroupSnapshots = new List<FormFieldGroupStatusSnapshot>(fieldGroupIds.Length);
        foreach (var fieldGroupId in fieldGroupIds)
        {
            fieldGroupSnapshots.Add(new FormFieldGroupStatusSnapshot(
                fieldGroupId,
                Relevant: true,
                Completed: IsFieldGroupCompleted(fieldGroupId, requiredFieldIdsByFieldGroupId, blockingFieldIds, effectiveBlockingFieldGroupIds, valuesByVariableId),
                OpenGroupErrorCodes: BuildOpenGroupErrorCodes(errorViolations, fieldGroupId)));
        }

        return fieldGroupSnapshots.ToImmutableArray();
    }

    private static ImmutableHashSet<string> BuildBlockingFieldIds(ImmutableArray<FormConstraintViolation> errorViolations)
        => errorViolations
            .Select(violation => violation.FieldId)
            .Where(fieldId => !string.IsNullOrWhiteSpace(fieldId))
            .ToImmutableHashSet(StringComparer.Ordinal);

    private static ImmutableArray<string> BuildBlockingFieldErrorCodes(
        ImmutableArray<FormConstraintViolation> errorViolations,
        string fieldId)
        => errorViolations
            .Where(violation => string.Equals(violation.FieldId, fieldId, StringComparison.Ordinal))
            .Select(violation => violation.Code)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToImmutableArray();

    private ImmutableArray<string> BuildOpenGroupErrorCodes(
        ImmutableArray<FormConstraintViolation> errorViolations,
        string fieldGroupId)
        => errorViolations
            .Where(violation => FormFieldGroupCompletionScopePolicy.BlocksCandidateFieldGroup(
                violation.Scope,
                fieldGroupId,
                fieldGroupCompletionService.ResolveViolationFieldGroupId(violation)))
            .Select(violation => violation.Code)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToImmutableArray();

    private bool IsFieldGroupCompleted(
        string fieldGroupId,
        IReadOnlyDictionary<string, ImmutableArray<string>> requiredFieldIdsByFieldGroupId,
        ImmutableHashSet<string> blockingFieldIds,
        ImmutableHashSet<string> effectiveBlockingFieldGroupIds,
        IReadOnlyDictionary<string, string> valuesByVariableId)
    {
        if (effectiveBlockingFieldGroupIds.Contains(fieldGroupId))
        {
            return false;
        }

        return !requiredFieldIdsByFieldGroupId.TryGetValue(fieldGroupId, out var requiredFieldIdsInGroup)
            || requiredFieldIdsInGroup.All(fieldId => fieldCompletionService.IsFieldCompleted(fieldId, blockingFieldIds, valuesByVariableId));
    }
}
