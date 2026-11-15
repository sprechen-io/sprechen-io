using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Stellt eine eigene Kopie der Navigationsfolge und den Fokuszugriff für Befehle bereit. Die Verfügbarkeit prüft der zuständige Navigationsweg.
/// </summary>
internal sealed class FormCqrsNavigationProjection(
    FormDialogDefinitionState definitionState,
    FormMutableStateStore mutableState,
    FormFieldVisibilityPolicy visibilityService)
{
    private readonly FormDialogDefinitionState _definitionState =
        definitionState ?? throw new ArgumentNullException(nameof(definitionState));
    private readonly FormMutableStateStore _mutableState =
        mutableState ?? throw new ArgumentNullException(nameof(mutableState));
    private readonly FormFieldVisibilityPolicy _visibilityService =
        visibilityService ?? throw new ArgumentNullException(nameof(visibilityService));

    internal FieldId? CurrentFocusedFieldId => _mutableState.FocusedFieldId;

    internal string[] OrderedFieldIds => _definitionState.OrderedFieldIds.ToArray();

    internal bool IsFieldInputAvailable(string fieldId)
        => _visibilityService.IsFieldInputAvailable(fieldId);

    internal bool TryGetFieldIndex(string fieldId, out int index)
        => _definitionState.FieldIndexById.TryGetValue(FieldId.From(fieldId), out index);

    internal void SetCurrentFocus(string fieldId)
    {
        if (string.IsNullOrWhiteSpace(fieldId))
        {
            return;
        }

        _mutableState.SetFocusedField(FieldId.From(fieldId));
    }
}
