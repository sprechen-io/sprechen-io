using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Zustandszugriff für Sichtbarkeitsänderungen an Feldern und Feldgruppen.
/// </summary>
internal interface IFormActionVisibilityState
{
    bool IsFieldVisible(FieldId fieldId);

    bool SetFieldVisible(FieldId fieldId, bool visible);

    bool IsFieldGroupVisible(FieldGroupId fieldGroupId);

    bool SetFieldGroupVisible(FieldGroupId fieldGroupId, bool visible);
}
