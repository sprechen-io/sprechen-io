using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft Kennung und Eingabeverfügbarkeit eines Fokusziels, bevor es im Zustand gesetzt wird. Ein bereits vorhandener Fokus kann ebenfalls erfolgreich sein.
/// </summary>
internal sealed class FormFieldFocusTargetResolver
{
    private readonly IFormNavigatorState state;

    public FormFieldFocusTargetResolver(IFormNavigatorState state)
    {
        this.state = state ?? throw new ArgumentNullException(nameof(state));
    }

    public bool TrySetFocus(string fieldId)
    {
        if (string.IsNullOrWhiteSpace(fieldId))
        {
            return false;
        }

        var normalizedFieldId = FieldId.From(fieldId);

        if (!state.IsKnownField(normalizedFieldId))
        {
            return false;
        }

        if (!state.IsFieldInputAvailable(normalizedFieldId))
        {
            return false;
        }

        state.SetCurrentFocus(normalizedFieldId);
        return true;
    }
}
