using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Liefert der Gruppennavigation die geordnete Feldfolge einer Gruppe einschließlich ihrer Untergruppen sowie die Prüfung der Gruppensichtbarkeit.
/// </summary>
internal sealed class FormCqrsFieldGroupNavigationProjection(
    FormDialogDefinitionState definitionState,
    FormFieldVisibilityPolicy visibilityService)
{
    private readonly FormDialogDefinitionState _definitionState =
        definitionState ?? throw new ArgumentNullException(nameof(definitionState));
    private readonly FormFieldVisibilityPolicy _visibilityService =
        visibilityService ?? throw new ArgumentNullException(nameof(visibilityService));

    internal bool IsFieldGroupKnown(string fieldGroupId)
    {
        if (string.IsNullOrWhiteSpace(fieldGroupId))
        {
            return false;
        }

        return _definitionState.FieldGroupsById.ContainsKey(FieldGroupId.From(fieldGroupId));
    }

    internal bool IsFieldGroupVisible(string fieldGroupId)
    {
        if (string.IsNullOrWhiteSpace(fieldGroupId))
        {
            return false;
        }

        return _definitionState.FieldGroupsById.ContainsKey(FieldGroupId.From(fieldGroupId))
            && _visibilityService.IsFieldGroupVisible(fieldGroupId);
    }

    internal IEnumerable<string> GetFieldsInFieldGroup(string fieldGroupId)
    {
        if (string.IsNullOrWhiteSpace(fieldGroupId))
        {
            return [];
        }

        if (!_definitionState.FieldGroupsById.ContainsKey(FieldGroupId.From(fieldGroupId)))
        {
            return [];
        }

        return _definitionState.OrderedFieldIds
            .Where(fieldId =>
                _definitionState.FieldToFieldGroupId.TryGetValue(FieldId.From(fieldId), out var fieldFieldGroupId)
                && _visibilityService.IsFieldGroupOnPath(fieldFieldGroupId, fieldGroupId))
            .ToArray();
    }
}
