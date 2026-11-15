using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Sucht das erste eingabeverfügbare Feld im Gruppenpfad als Fokusziel, damit ein Gruppensprung nicht auf einem reinen Anzeigefeld landet.
/// </summary>
internal sealed class FormFieldGroupFocusResolver
{
    private readonly IFormNavigatorState state;

    public FormFieldGroupFocusResolver(IFormNavigatorState state)
    {
        this.state = state ?? throw new ArgumentNullException(nameof(state));
    }

    public bool TryNavigateToFieldGroup(string fieldGroupId)
    {
        if (string.IsNullOrWhiteSpace(fieldGroupId))
        {
            return false;
        }

        var normalizedFieldGroupId = FieldGroupId.From(fieldGroupId);

        if (!state.IsKnownFieldGroup(normalizedFieldGroupId))
        {
            return false;
        }

        foreach (var fieldId in state.OrderedFieldIds)
        {
            var typedFieldId = FieldId.From(fieldId);
            if (IsFieldInTargetFieldGroupPath(typedFieldId, normalizedFieldGroupId))
            {
                state.SetCurrentFocus(typedFieldId);
                return true;
            }
        }

        return false;
    }

    private bool IsFieldInTargetFieldGroupPath(FieldId fieldId, FieldGroupId fieldGroupId)
    {
        // Das Fokusziel anhand der Eingabeverfügbarkeit wählen. Ein ausschließlich
        // vorlesbares Anzeigefeld ist kein Ziel für den Eingabefokus.
        if (!state.IsFieldInputAvailable(fieldId))
        {
            return false;
        }

        var currentFieldGroupId = state.GetFieldGroupId(fieldId);
        return currentFieldGroupId is { } current
            && state.IsFieldGroupOnPath(current, fieldGroupId);
    }
}
