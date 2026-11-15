using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Ergänzt modellierte Anfangswerte nur bei bisher fehlendem Variablenwert, damit bereits vorhandene Angaben erhalten bleiben.
/// </summary>
internal static class FormInitialValueApplier
{
    public static void ApplyMissingInitialValues(
        ImmutableDictionary<VariableId, VariableDefinition> variablesById,
        FormMutableStateStore mutableState)
    {
        ArgumentNullException.ThrowIfNull(variablesById);
        ArgumentNullException.ThrowIfNull(mutableState);

        foreach (var (variableId, variable) in variablesById)
        {
            if (mutableState.GetVariableValue(variableId) is not null)
            {
                continue;
            }

            if (variable.InitialValue is null)
            {
                continue;
            }

            var element = variable.InitialValue.Value;
            if (!FormActionJsonValueEncoder.TryEncodeJsonValue(element, out var encoded))
            {
                continue;
            }

            mutableState.SetVariableValue(variableId, encoded);
        }
    }
}
