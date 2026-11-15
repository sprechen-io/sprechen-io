using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Begrenzt den Zustandszugriff von Feldaktionen auf Aktivierung und Schreibschutz, damit diese Ausführer keine übrigen Laufzeitdaten benötigen.
/// </summary>
internal interface IFormActionFieldAccessState
{
    bool IsFieldEnabled(FieldId fieldId);

    bool SetFieldEnabled(FieldId fieldId, bool enabled);

    bool IsFieldReadOnly(FieldId fieldId);

    bool SetFieldReadOnly(FieldId fieldId, bool readOnly);
}
