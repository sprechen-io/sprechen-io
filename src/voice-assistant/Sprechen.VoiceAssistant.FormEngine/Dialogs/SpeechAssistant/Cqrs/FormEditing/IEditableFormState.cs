using Sprechen.Common.Cqrs;
using Sprechen.Mda.Contracts.V1.Identifiers;
namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormEditing;

/// <summary>
/// Stellt die Feldzuordnung und den Zugriff auf Variablenwerte für Bearbeitungsbefehle bereit.
/// Die unterschiedlichen Typen <see cref="FieldId"/> und <see cref="VariableId"/> trennen die
/// beiden Kennungsrollen bereits an der Schnittstelle.
/// </summary>
public interface IEditableFormState
{
    bool IsFieldKnown(FieldId fieldId);

    bool TryGetVariableId(FieldId fieldId, out VariableId variableId);

    string? GetCurrentValue(VariableId variableId);

    void SetValue(VariableId variableId, string value);
}

