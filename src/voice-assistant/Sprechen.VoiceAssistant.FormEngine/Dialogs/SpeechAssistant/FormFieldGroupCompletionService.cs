using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Ermittelt blockierte Feldgruppen und zählt abgeschlossene Gruppen anhand ihrer Pflichtfelder.
/// </summary>
internal sealed class FormFieldGroupCompletionService(
    ImmutableDictionary<FieldId, string> fieldToFieldGroupId,
    ImmutableArray<ConstraintDefinition> constraints,
    FormFieldCompletionPolicy fieldCompletionService)
{
    private readonly ImmutableDictionary<FieldId, string> _fieldToFieldGroupId = fieldToFieldGroupId ?? throw new ArgumentNullException(nameof(fieldToFieldGroupId));
    private readonly ImmutableArray<ConstraintDefinition> _constraints = constraints;
    private readonly FormFieldCompletionPolicy _fieldCompletionService = fieldCompletionService ?? throw new ArgumentNullException(nameof(fieldCompletionService));

    public int CountCompletedFieldGroups(
        ImmutableArray<string> fieldGroupIds,
        ImmutableArray<string> requiredFieldIds,
        ImmutableHashSet<string> blockingFieldIds,
        ImmutableHashSet<string> blockingFieldGroupIds,
        IReadOnlyDictionary<string, string> valuesByVariableId)
    {
        ArgumentNullException.ThrowIfNull(blockingFieldIds);
        ArgumentNullException.ThrowIfNull(blockingFieldGroupIds);
        ArgumentNullException.ThrowIfNull(valuesByVariableId);

        if (fieldGroupIds.Length == 0)
        {
            return 0;
        }

        var effectiveBlockingFieldGroupIds = BuildEffectiveBlockingFieldGroupIds(blockingFieldIds, blockingFieldGroupIds);
        var requiredFieldIdsByFieldGroupId = GroupRequiredFieldIdsByFieldGroup(requiredFieldIds);

        var completedFieldGroups = 0;

        foreach (var fieldGroupId in fieldGroupIds)
        {
            if (effectiveBlockingFieldGroupIds.Contains(fieldGroupId))
            {
                continue;
            }

            if (requiredFieldIdsByFieldGroupId.TryGetValue(fieldGroupId, out var requiredFieldIdsInGroup)
                && requiredFieldIdsInGroup.Any(fieldId => !_fieldCompletionService.IsFieldCompleted(fieldId, blockingFieldIds, valuesByVariableId)))
            {
                continue;
            }

            completedFieldGroups++;
        }

        return completedFieldGroups;
    }

    public ImmutableHashSet<string> BuildEffectiveBlockingFieldGroupIds(
        ImmutableHashSet<string> blockingFieldIds,
        ImmutableHashSet<string> blockingFieldGroupIds)
    {
        ArgumentNullException.ThrowIfNull(blockingFieldIds);
        ArgumentNullException.ThrowIfNull(blockingFieldGroupIds);

        var blockingFieldGroupIdsByFieldViolation = blockingFieldIds
            .Select(fieldId => _fieldToFieldGroupId.TryGetValue(FieldId.From(fieldId), out var fieldGroupId) ? fieldGroupId : null)
            .Where(fieldGroupId => !string.IsNullOrWhiteSpace(fieldGroupId))
            .OfType<string>()
            .ToImmutableHashSet(StringComparer.Ordinal);

        return blockingFieldGroupIdsByFieldViolation
            .Union(blockingFieldGroupIds)
            .ToImmutableHashSet(StringComparer.Ordinal);
    }

    public ImmutableHashSet<string> BuildScopedBlockingFieldGroupIds(
        ImmutableArray<FormConstraintViolation> errorViolations,
        ImmutableArray<string> relevantFieldGroupIds)
    {
        if (errorViolations.Length == 0 || relevantFieldGroupIds.Length == 0)
        {
            return ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal);
        }

        var constraintById = _constraints
            .Where(constraint => constraint is not null && !string.IsNullOrWhiteSpace(constraint.ConstraintId))
            .GroupBy(constraint => constraint.ConstraintId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var blockingFieldGroupIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var violation in errorViolations)
        {
            if (!constraintById.TryGetValue(violation.ConstraintId, out var constraint))
            {
                continue;
            }

            var scope = FormConstraintScopeResolver.Resolve(constraint);
            var scopedFieldGroupId = ResolveViolationFieldGroupId(violation);

            foreach (var fieldGroupId in relevantFieldGroupIds)
            {
                if (FormFieldGroupCompletionScopePolicy.BlocksCandidateFieldGroup(scope, fieldGroupId, scopedFieldGroupId))
                {
                    _ = blockingFieldGroupIds.Add(fieldGroupId);
                }
            }
        }

        return blockingFieldGroupIds.ToImmutableHashSet(StringComparer.Ordinal);
    }

    public ImmutableDictionary<string, ImmutableArray<string>> GroupRequiredFieldIdsByFieldGroup(
        ImmutableArray<string> requiredFieldIds)
    {
        return requiredFieldIds
            .Where(fieldId => _fieldToFieldGroupId.ContainsKey(FieldId.From(fieldId)))
            .GroupBy(fieldId => _fieldToFieldGroupId[FieldId.From(fieldId)], StringComparer.Ordinal)
            .ToImmutableDictionary(group => group.Key, group => group.ToImmutableArray(), StringComparer.Ordinal);
    }

    public string? ResolveViolationFieldGroupId(FormConstraintViolation violation)
    {
        ArgumentNullException.ThrowIfNull(violation);

        if (!string.IsNullOrWhiteSpace(violation.FieldGroupId))
        {
            return violation.FieldGroupId.Trim();
        }

        return _fieldToFieldGroupId.TryGetValue(FieldId.From(violation.FieldId), out var fieldGroupId)
            ? fieldGroupId
            : null;
    }
}
