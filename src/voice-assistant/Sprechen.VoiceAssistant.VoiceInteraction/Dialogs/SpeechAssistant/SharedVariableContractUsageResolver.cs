using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Ermittelt die Verwendung gemeinsamer Variablen im Formularvertrag für die anschließende Übernahme und Herkunftsansage.
/// </summary>
internal sealed class SharedVariableContractUsageResolver
{
    public bool IsVariableUsedInContract(FormContract contract, string variableId)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentException.ThrowIfNullOrWhiteSpace(variableId);

        if (contract.Variables.Any(variable => string.Equals(variable.VariableId, variableId, StringComparison.Ordinal)))
        {
            return true;
        }

        foreach (var field in contract.Fields)
        {
            if (field is null)
            {
                continue;
            }

            if (string.Equals(ResolveFieldVariableId(field), variableId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string ResolveFieldVariableId(FieldDefinition field)
        => field.VariableId == default
            ? field.FieldId
            : field.VariableId.Value;
}
