using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Verbindet Kennungsprüfung und Variablenzugriff für Bearbeitungsbefehle. Die Änderungsrückmeldung folgt nach der Zustandsänderung.
/// </summary>
internal sealed class FormCqrsEditingProjection(
    FormDialogDefinitionState definitionState,
    FormMutableStateStore mutableState,
    Action onValueChanged)
{
    private readonly FormDialogDefinitionState _definitionState =
        definitionState ?? throw new ArgumentNullException(nameof(definitionState));
    private readonly FormMutableStateStore _mutableState =
        mutableState ?? throw new ArgumentNullException(nameof(mutableState));
    private readonly Action _onValueChanged = onValueChanged ?? throw new ArgumentNullException(nameof(onValueChanged));

    internal bool IsFieldKnown(string fieldId)
        => _definitionState.FieldsById.ContainsKey(FieldId.From(fieldId));

    internal bool TryGetVariableId(string fieldId, out string variableId)
        => _definitionState.FieldToVariableId.TryGetValue(FieldId.From(fieldId), out variableId!);

    internal string? GetCurrentValue(string variableId)
        => _mutableState.ValuesByVariableId.TryGetValue(variableId, out var value)
            ? value
            : null;

    internal void SetValue(string variableId, string value)
    {
        if (!_definitionState.VariablesById.ContainsKey(VariableId.From(variableId)))
        {
            throw new InvalidOperationException("A field value used a variable identifier that is not an exact canonical key.");
        }

        _mutableState.SetVariableValue(variableId, value);
        _onValueChanged();
    }
}
