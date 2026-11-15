using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft die Verfügbarkeit von Sprungzielen für hörbare Inhalte. Diese Freigabe allein garantiert noch kein Ziel für den Eingabefokus.
/// </summary>
internal sealed class FormJumpTargetAvailability
{
    private readonly IFormNavigatorState navigatorState;

    public FormJumpTargetAvailability(IFormNavigatorState navigatorState)
    {
        this.navigatorState = navigatorState ?? throw new ArgumentNullException(nameof(navigatorState));
    }

    public bool IsNavigableField(string fieldId)
    {
        if (string.IsNullOrWhiteSpace(fieldId))
        {
            return false;
        }

        var typedFieldId = FieldId.From(fieldId);
        return navigatorState.IsKnownField(typedFieldId) && navigatorState.IsFieldAudiblyAvailable(typedFieldId);
    }

    public bool IsNavigableFieldGroup(string fieldGroupId)
    {
        if (string.IsNullOrWhiteSpace(fieldGroupId))
        {
            return false;
        }

        var typedFieldGroupId = FieldGroupId.From(fieldGroupId);
        if (!navigatorState.IsKnownFieldGroup(typedFieldGroupId))
        {
            return false;
        }

        foreach (var fieldId in navigatorState.OrderedFieldIds)
        {
            var typedFieldId = FieldId.From(fieldId);
            if (!navigatorState.IsFieldAudiblyAvailable(typedFieldId))
            {
                continue;
            }

            var currentFieldGroupId = navigatorState.GetFieldGroupId(typedFieldId);
            if (currentFieldGroupId is not { } current)
            {
                continue;
            }

            if (navigatorState.IsFieldGroupOnPath(current, typedFieldGroupId))
            {
                return true;
            }
        }

        return false;
    }
}
