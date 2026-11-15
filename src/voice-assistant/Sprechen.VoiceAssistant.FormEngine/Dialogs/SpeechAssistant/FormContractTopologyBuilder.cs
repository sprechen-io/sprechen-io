using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Ermittelt die geordnete Formularstruktur bei jedem Aufruf neu, ohne bereits besuchte Gruppen oder Felder aus einem früheren Aufruf zu übernehmen.
/// </summary>
internal static class FormContractTopologyBuilder
{
    public static FormContractTopology Build(
        FormContract contract,
        ImmutableDictionary<FieldGroupId, FieldGroupDefinition> fieldGroupsById,
        ImmutableDictionary<FieldId, FieldDefinition> fieldsById)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(fieldGroupsById);
        ArgumentNullException.ThrowIfNull(fieldsById);

        return new FormContractTopologyTraversal(fieldGroupsById, fieldsById).Build(contract);
    }
}
