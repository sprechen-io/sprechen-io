using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Vergleicht Relevanz und Abschluss vorhandener Felder und Gruppen.
/// Neue Einträge werden ebenfalls gemeldet. Die feste Reihenfolge macht
/// zusammengehörige Beobachtungen reproduzierbar.
/// </summary>
internal sealed class EvaluationTelemetryDetailedStatusChangeDetector(EvaluationStatusTelemetryPayloads payloads)
{
    private readonly EvaluationStatusTelemetryPayloads _payloads =
        payloads ?? throw new ArgumentNullException(nameof(payloads));

    internal IReadOnlyList<EvaluationTelemetryDetailedStatusChange> DetectChanges(
        EvaluationDetailedFormStatus previousSnapshot,
        EvaluationDetailedFormStatus snapshot)
    {
        ArgumentNullException.ThrowIfNull(previousSnapshot);
        ArgumentNullException.ThrowIfNull(snapshot);

        var changes = new List<EvaluationTelemetryDetailedStatusChange>();
        AddFieldChanges(previousSnapshot, snapshot, changes);
        AddFieldGroupChanges(previousSnapshot, snapshot, changes);
        return changes;
    }

    private void AddFieldChanges(
        EvaluationDetailedFormStatus previousSnapshot,
        EvaluationDetailedFormStatus snapshot,
        List<EvaluationTelemetryDetailedStatusChange> changes)
    {
        var previousFieldsById = previousSnapshot.Fields.ToDictionary(
            field => field.FieldId,
            StringComparer.Ordinal);

        foreach (var field in snapshot.Fields.OrderBy(field => field.FieldId, StringComparer.Ordinal))
        {
            var hasPreviousField = previousFieldsById.TryGetValue(field.FieldId, out var previousField);
            var relevanceChanged = !hasPreviousField || previousField!.Relevant != field.Relevant;
            var completionChanged = !hasPreviousField
                || previousField!.Completed != field.Completed;

            if (relevanceChanged)
            {
                changes.Add(new EvaluationTelemetryDetailedStatusChange(
                    TelemetryEventNames.FieldRelevanceChanged,
                    _payloads.CreateFieldRelevanceChanged(field)));
            }

            if (completionChanged)
            {
                changes.Add(new EvaluationTelemetryDetailedStatusChange(
                    TelemetryEventNames.FieldCompletionChanged,
                    _payloads.CreateFieldCompletionChanged(field)));
            }
        }
    }

    private void AddFieldGroupChanges(
        EvaluationDetailedFormStatus previousSnapshot,
        EvaluationDetailedFormStatus snapshot,
        List<EvaluationTelemetryDetailedStatusChange> changes)
    {
        var previousGroupsById = previousSnapshot.FieldGroups.ToDictionary(
            group => group.FieldGroupId,
            StringComparer.Ordinal);

        foreach (var fieldGroup in snapshot.FieldGroups.OrderBy(group => group.FieldGroupId, StringComparer.Ordinal))
        {
            var hasPreviousGroup = previousGroupsById.TryGetValue(fieldGroup.FieldGroupId, out var previousGroup);
            var relevanceChanged = !hasPreviousGroup || previousGroup!.Relevant != fieldGroup.Relevant;
            var completionChanged = !hasPreviousGroup
                || previousGroup!.Completed != fieldGroup.Completed;

            if (relevanceChanged)
            {
                changes.Add(new EvaluationTelemetryDetailedStatusChange(
                    TelemetryEventNames.FieldGroupRelevanceChanged,
                    _payloads.CreateFieldGroupRelevanceChanged(fieldGroup)));
            }

            if (completionChanged)
            {
                changes.Add(new EvaluationTelemetryDetailedStatusChange(
                    TelemetryEventNames.FieldGroupCompletionChanged,
                    _payloads.CreateFieldGroupCompletionChanged(fieldGroup)));
            }
        }
    }

}
