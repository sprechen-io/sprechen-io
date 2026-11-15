using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Übernimmt bekannte Anfangswerte nacheinander in den Zustand. Spätere ungültige Kennungen machen frühere Übernahmen nicht automatisch rückgängig.
/// </summary>
internal sealed class FormInitialRuntimeValueLoader
{
    internal void ApplyKnownValues(
        IReadOnlyDictionary<VariableId, VariableDefinition> variablesById,
        FormMutableStateStore mutableState,
        IReadOnlyDictionary<string, string> initialValuesByVariableId)
    {
        ArgumentNullException.ThrowIfNull(variablesById);
        ArgumentNullException.ThrowIfNull(mutableState);
        ArgumentNullException.ThrowIfNull(initialValuesByVariableId);

        foreach (var (variableId, value) in initialValuesByVariableId)
        {
            if (!variablesById.ContainsKey(VariableId.From(variableId)))
            {
                throw new InvalidOperationException("An initial value used a variable identifier that is not an exact canonical key.");
            }

            mutableState.SetVariableValue(variableId, value);
        }
    }
}
