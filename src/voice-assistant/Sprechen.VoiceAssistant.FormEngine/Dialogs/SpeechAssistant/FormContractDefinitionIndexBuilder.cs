using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Indiziert Variablen, Felder und Feldgruppen nach Kennung und weist doppelte Kennungen zurück. Fehlende Einträge und Standardkennungen werden übersprungen.
/// </summary>
internal static class FormContractDefinitionIndexBuilder
{
    public static FormContractDefinitionIndexes Build(FormContract contract)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return new FormContractDefinitionIndexes(
            IndexVariables(contract.Variables),
            IndexFields(contract.Fields),
            IndexFieldGroups(contract.FieldGroups));
    }

    private static ImmutableDictionary<VariableId, VariableDefinition> IndexVariables(
        IEnumerable<VariableDefinition> variables)
    {
        var map = ImmutableDictionary.CreateBuilder<VariableId, VariableDefinition>();
        foreach (var variable in variables)
        {
            if (variable is null || variable.VariableId == default)
            {
                continue;
            }

            if (!map.TryAdd(variable.VariableId, variable))
            {
                throw new InvalidOperationException("A modeled identifier was not an exact unique canonical key.");
            }
        }

        return map.ToImmutable();
    }

    private static ImmutableDictionary<FieldId, FieldDefinition> IndexFields(IEnumerable<FieldDefinition> fields)
    {
        var map = ImmutableDictionary.CreateBuilder<FieldId, FieldDefinition>();
        foreach (var field in fields)
        {
            if (field is null || field.FieldId == default)
            {
                continue;
            }

            if (!map.TryAdd(field.FieldId, field))
            {
                throw new InvalidOperationException("A modeled identifier was not an exact unique canonical key.");
            }
        }

        return map.ToImmutable();
    }

    private static ImmutableDictionary<FieldGroupId, FieldGroupDefinition> IndexFieldGroups(
        IEnumerable<FieldGroupDefinition> fieldGroups)
    {
        var map = ImmutableDictionary.CreateBuilder<FieldGroupId, FieldGroupDefinition>();
        foreach (var fieldGroup in fieldGroups)
        {
            if (fieldGroup is null || fieldGroup.FieldGroupId == default)
            {
                continue;
            }

            if (!map.TryAdd(fieldGroup.FieldGroupId, fieldGroup))
            {
                throw new InvalidOperationException("A modeled identifier was not an exact unique canonical key.");
            }
        }

        return map.ToImmutable();
    }
}


