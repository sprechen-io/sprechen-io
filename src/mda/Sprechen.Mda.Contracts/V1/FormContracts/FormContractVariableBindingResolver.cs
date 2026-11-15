using System.Text.Json;
using Sprechen.Mda.Contracts.V1.Variables;

namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Löst Variablenbindungen gegen wiederverwendbare Definitionen auf und übernimmt
/// Startwerte, Einschränkungen und Metadaten der jeweiligen Bindung.
/// </summary>
internal static class FormContractVariableBindingResolver
{
    internal static List<VariableDefinition> Resolve(
        IReadOnlyList<FormVariableBindingDefinition> bindings,
        IReadOnlyDictionary<string, ReusableVariableDefinition> definitionsById)
    {
        var resolvedVariables = new List<VariableDefinition>(bindings.Count);
        foreach (var binding in bindings)
        {
            if (!definitionsById.TryGetValue(binding.VariableDefinitionId, out var definition))
            {
                throw new InvalidOperationException(
                    $"FormContract references missing variableDefinitionId '{binding.VariableDefinitionId}'.");
            }

            resolvedVariables.Add(new VariableDefinition
            {
                VariableId = binding.VariableId,
                Name = definition.Name,
                Description = FormContractResolutionJsonCloner.CloneOptionalLocalizedText(definition.Description),
                Unit = definition.Unit,
                SemanticTag = definition.SemanticTag,
                PrivacyClass = definition.PrivacyClass,
                SensitiveDataPolicy = FormContractResolutionJsonCloner.CloneSensitiveDataPolicy(definition.SensitiveDataPolicy),
                ValueType = FormContractResolutionJsonCloner.CloneJsonElement(definition.ValueType),
                InitialValue = FormContractResolutionJsonCloner.CloneJsonElement(binding.InitialValue),
                InvariantConstraints = CloneVariableConstraints(binding.InvariantConstraints),
                Trace = FormContractResolutionJsonCloner.CloneTraceInfo(binding.Trace),
                Extensions = FormContractResolutionJsonCloner.CloneExtensions(binding.Extensions)
            });
        }

        return resolvedVariables;
    }

    private static List<JsonElement>? CloneVariableConstraints(List<ConstraintDefinition>? constraints)
    {
        if (constraints is null)
        {
            return null;
        }

        var clones = new List<JsonElement>(constraints.Count);
        foreach (var constraint in constraints)
        {
            clones.Add(FormContractResolutionJsonCloner.CloneConstraintJsonElement(constraint));
        }

        return clones;
    }
}
