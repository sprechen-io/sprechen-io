using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Bestimmt Pflichtstatus und Abschluss eines Felds aus Bedingungen, Wert und blockierenden Fehlern für die Statusprojektion.
/// </summary>
internal sealed class FormFieldCompletionPolicy(
    ImmutableDictionary<FieldId, FieldDefinition> fieldsById,
    ImmutableDictionary<FieldId, string> fieldToVariableId,
    ImmutableDictionary<VariableId, VariableDefinition> variablesById,
    ImmutableArray<ConstraintDefinition> constraints)
{
    private readonly ImmutableDictionary<FieldId, FieldDefinition> _fieldsById = fieldsById ?? throw new ArgumentNullException(nameof(fieldsById));
    private readonly ImmutableDictionary<FieldId, string> _fieldToVariableId = fieldToVariableId ?? throw new ArgumentNullException(nameof(fieldToVariableId));
    private readonly ImmutableDictionary<VariableId, VariableDefinition> _variablesById = variablesById ?? throw new ArgumentNullException(nameof(variablesById));
    private readonly ImmutableArray<ConstraintDefinition> _constraints = constraints;

    public bool IsRequiredField(string fieldId)
    {
        if (!_fieldsById.TryGetValue(FieldId.From(fieldId), out var field))
        {
            return false;
        }

        if (field.FieldConstraints is not null
            && field.FieldConstraints.Any(constraint => constraint.Kind == ConstraintKind.Required))
        {
            return true;
        }

        var variableId = _fieldToVariableId.TryGetValue(FieldId.From(fieldId), out var resolvedVariableId)
            ? resolvedVariableId
            : string.Empty;

        return _constraints.Any(constraint =>
            constraint.Kind == ConstraintKind.Required
            && (string.Equals(constraint.Target.Kind, TargetKinds.Field, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(constraint.Target.FieldId, fieldId, StringComparison.Ordinal)
                || string.Equals(constraint.Target.Kind, TargetKinds.Variable, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(constraint.Target.VariableId, variableId, StringComparison.Ordinal)
                || string.Equals(constraint.Target.Kind, TargetKinds.Form, StringComparison.OrdinalIgnoreCase)));
    }

    public bool IsFieldCompleted(
        string fieldId,
        ImmutableHashSet<string> blockingFieldIds,
        IReadOnlyDictionary<string, string> valuesByVariableId)
    {
        ArgumentNullException.ThrowIfNull(blockingFieldIds);
        ArgumentNullException.ThrowIfNull(valuesByVariableId);

        if (blockingFieldIds.Contains(fieldId))
        {
            return false;
        }

        if (!_fieldToVariableId.TryGetValue(FieldId.From(fieldId), out var variableId))
        {
            return false;
        }

        var valueTypeKind = FormVariableValueReader.TryGetValueTypeKind(_variablesById, variableId)
            ?? ValueTypeKinds.String;
        return FormVariableValueReader.IsVariableValueSet(
            _variablesById,
            valuesByVariableId,
            variableId,
            valueTypeKind);
    }

    public bool IsRequiredViolation(FormConstraintViolation violation)
    {
        ArgumentNullException.ThrowIfNull(violation);

        foreach (var constraint in _constraints)
        {
            if (!string.Equals(constraint.ConstraintId, violation.ConstraintId, StringComparison.Ordinal))
            {
                continue;
            }

            return constraint.Kind == ConstraintKind.Required;
        }

        return false;
    }
}
