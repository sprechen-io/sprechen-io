using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Feldreihenfolge, Bedingungen, Definitionszuordnungen und Wertezugriff für die Formularvalidierung.
/// </summary>
internal sealed record FormDialogValidationContext
{
    internal FormDialogValidationContext(
        ImmutableArray<string> orderedFieldIds,
        ImmutableArray<ConstraintDefinition> constraints,
        ImmutableDictionary<FieldId, FieldDefinition> fieldsById,
        ImmutableDictionary<FieldId, string> fieldToVariableId,
        ImmutableDictionary<FieldId, string> fieldToFieldGroupId,
        ImmutableDictionary<VariableId, VariableDefinition> variablesById,
        IReadOnlyDictionary<string, string> valuesByVariableId)
    {
        OrderedFieldIds = orderedFieldIds;
        Constraints = constraints;
        FieldsById = fieldsById ?? throw new ArgumentNullException(nameof(fieldsById));
        FieldToVariableId = fieldToVariableId ?? throw new ArgumentNullException(nameof(fieldToVariableId));
        FieldToFieldGroupId = fieldToFieldGroupId ?? throw new ArgumentNullException(nameof(fieldToFieldGroupId));
        VariablesById = variablesById ?? throw new ArgumentNullException(nameof(variablesById));
        ValuesByVariableId = valuesByVariableId ?? throw new ArgumentNullException(nameof(valuesByVariableId));
    }

    public ImmutableArray<string> OrderedFieldIds { get; }

    public ImmutableArray<ConstraintDefinition> Constraints { get; }

    public ImmutableDictionary<FieldId, FieldDefinition> FieldsById { get; }

    public ImmutableDictionary<FieldId, string> FieldToVariableId { get; }

    public ImmutableDictionary<FieldId, string> FieldToFieldGroupId { get; }

    public ImmutableDictionary<VariableId, VariableDefinition> VariablesById { get; }

    public IReadOnlyDictionary<string, string> ValuesByVariableId { get; }
}
