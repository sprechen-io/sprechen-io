using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Fokus- und Gruppennavigationsversuche, die Regelaktionen auslösen können.
/// </summary>
internal interface IFormActionNavigationState
{
    bool TryNavigateToFieldGroup(FieldGroupId fieldGroupId);

    bool TrySetFocus(FieldId fieldId);
}
