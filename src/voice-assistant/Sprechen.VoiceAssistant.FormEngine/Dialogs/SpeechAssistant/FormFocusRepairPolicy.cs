using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Behält einen verfügbaren Fokus oder sucht vorwärts, rückwärts und ab Beginn ein Ersatzfeld. Ohne verfügbares Ziel bleibt der bisherige Fokus bestehen.
/// </summary>
internal sealed class FormFocusRepairPolicy
{
    private readonly IFormNavigatorState state;

    public FormFocusRepairPolicy(IFormNavigatorState state)
    {
        this.state = state ?? throw new ArgumentNullException(nameof(state));
    }

    public void EnsureFocusIsValid()
    {
        var currentFocus = state.CurrentFocusedFieldId;
        if (currentFocus is { } current && state.IsFieldInputAvailable(current))
        {
            return;
        }

        if (TryRepairFromCurrentFocus(currentFocus))
        {
            return;
        }

        SetFirstAvailableFocus();
    }

    private bool TryRepairFromCurrentFocus(FieldId? currentFocus)
    {
        if (currentFocus is not { } current || !state.TryGetFieldIndex(current, out var currentIndex))
        {
            return false;
        }

        return TrySetAvailableFocusForward(currentIndex + 1)
            || TrySetAvailableFocusBackward(currentIndex - 1);
    }

    private bool TrySetAvailableFocusForward(int startIndex)
    {
        var orderedFields = state.OrderedFieldIds;
        for (var i = startIndex; i < orderedFields.Length; i++)
        {
            if (TrySetAvailableFocus(orderedFields[i]))
            {
                return true;
            }
        }

        return false;
    }

    private bool TrySetAvailableFocusBackward(int startIndex)
    {
        var orderedFields = state.OrderedFieldIds;
        for (var i = startIndex; i >= 0; i--)
        {
            if (TrySetAvailableFocus(orderedFields[i]))
            {
                return true;
            }
        }

        return false;
    }

    private void SetFirstAvailableFocus()
    {
        foreach (var fieldId in state.OrderedFieldIds)
        {
            if (TrySetAvailableFocus(fieldId))
            {
                return;
            }
        }
    }

    private bool TrySetAvailableFocus(string fieldId)
    {
        var typedFieldId = FieldId.From(fieldId);
        if (!state.IsFieldInputAvailable(typedFieldId))
        {
            return false;
        }

        state.SetCurrentFocus(typedFieldId);
        return true;
    }
}
