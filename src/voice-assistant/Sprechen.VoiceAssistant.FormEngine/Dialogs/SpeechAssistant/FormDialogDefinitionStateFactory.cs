using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Erzeugt die Definitionsgrundlage mit Sprachangabe, Datenschutzregeln und Positionsindizes, bevor Laufzeitdienste sie übernehmen.
/// </summary>
internal static class FormDialogDefinitionStateFactory
{
    public static FormDialogDefinitionState Create(
        FormContract contract,
        string requestedIsoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedIsoCode);

        var normalizedIsoCode = requestedIsoCode.Trim();
        var definitionIndexes = FormContractDefinitionIndexBuilder.Build(contract);
        var topology = FormContractTopologyBuilder.Build(contract, definitionIndexes.FieldGroupsById, definitionIndexes.FieldsById);
        var policyResolver = new EffectiveSensitiveDataPolicyResolver();
        var effectivePolicies = ImmutableDictionary.CreateBuilder<VariableId, EffectiveSensitiveDataPolicy>();
        foreach (var (variableId, variable) in definitionIndexes.VariablesById)
        {
            if (!policyResolver.TryResolve(
                    variable.PrivacyClass,
                    variable.SensitiveDataPolicy,
                    out var policy,
                    out var failure))
            {
                throw new FormContractPolicyInvalidException(failure);
            }

            effectivePolicies.Add(variableId, policy!);
        }

        return new FormDialogDefinitionState
        {
            Contract = contract,
            PolitenessLevel = politenessLevel,
            RequestedIsoCode = normalizedIsoCode,
            LocalizationResolver = new FormLocalizationResolver(
                normalizedIsoCode,
                contract.DefaultLanguage,
                contract.SupportedLanguages,
                contract.StrictLocalization,
                politenessLevel,
                definitionIndexes.VariablesById,
                definitionIndexes.FieldGroupsById),
            VariablesById = definitionIndexes.VariablesById,
            FieldsById = definitionIndexes.FieldsById,
            FieldGroupsById = definitionIndexes.FieldGroupsById,
            OrderedFieldGroupIds = topology.OrderedFieldGroupIds,
            FieldGroupIndexById = topology.OrderedFieldGroupIds
                .Select((fieldGroupId, index) => (fieldGroupId, index))
                .ToImmutableDictionary(pair => FieldGroupId.From(pair.fieldGroupId), pair => pair.index),
            ParentFieldGroupById = topology.ParentFieldGroupById,
            OrderedFieldIds = topology.OrderedFieldIds,
            FieldIndexById = topology.OrderedFieldIds
                .Select((fieldId, index) => (fieldId, index))
                .ToImmutableDictionary(pair => FieldId.From(pair.fieldId), pair => pair.index),
            FieldToFieldGroupId = topology.FieldToFieldGroupId,
            FieldToVariableId = topology.FieldToVariableId,
            EffectivePoliciesByVariableId = effectivePolicies.ToImmutable(),
            Constraints = FormConstraintCollectionBuilder.Build(contract)
        };
    }
}


