using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Begrenzt den Zugriff von Variablenaktionen auf Lesen, Setzen und Löschen, damit die Zustandspflege beim Speicher bleibt.
/// </summary>
internal interface IFormActionVariableValueState
{
    string? GetVariableValue(VariableId variableId);

    bool SetVariableValue(VariableId variableId, string value);

    bool ClearVariableValue(VariableId variableId);
}
