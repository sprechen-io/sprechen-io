using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Liefert Bezeichnungen und gespeicherte Werte für Zusammenfassungen. Sichtbarkeit und Erlaubnis zum Vorlesen werden hier nicht geprüft.
/// </summary>
internal sealed class FormCqrsSummaryProjection(
    FormDialogDefinitionState definitionState,
    FormMutableStateStore mutableState)
{
    private readonly FormDialogDefinitionState _definitionState =
        definitionState ?? throw new ArgumentNullException(nameof(definitionState));
    private readonly FormMutableStateStore _mutableState =
        mutableState ?? throw new ArgumentNullException(nameof(mutableState));

    internal ImmutableArray<string> OrderedFieldIds => _definitionState.OrderedFieldIds;

    internal string? GetFieldLabel(string fieldId)
    {
        if (string.IsNullOrWhiteSpace(fieldId))
        {
            return null;
        }

        return _definitionState.FieldsById.TryGetValue(FieldId.From(fieldId), out var field)
            ? _definitionState.LocalizationResolver.ResolveText(field.Label, fieldId)
            : null;
    }

    internal string? GetFieldValue(string fieldId)
    {
        if (string.IsNullOrWhiteSpace(fieldId))
        {
            return null;
        }

        if (!_definitionState.FieldToVariableId.TryGetValue(FieldId.From(fieldId), out var variableId))
        {
            return null;
        }

        return _mutableState.ValuesByVariableId.TryGetValue(variableId, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : null;
    }
}
