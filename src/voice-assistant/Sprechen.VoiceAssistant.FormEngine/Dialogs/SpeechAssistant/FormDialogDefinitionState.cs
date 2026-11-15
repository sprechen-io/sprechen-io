using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Hält Definitionen, Kennungsindizes und erschlossene Formularstruktur zusammen, damit die Laufzeit dieselbe Definitionsgrundlage verwendet.
/// </summary>
internal sealed class FormDialogDefinitionState
{
    public required FormContract Contract { get; init; }

    public required PolitenessLevel PolitenessLevel { get; init; }

    public required string RequestedIsoCode { get; init; }

    public required FormLocalizationResolver LocalizationResolver { get; init; }

    public required ImmutableDictionary<VariableId, VariableDefinition> VariablesById { get; init; }

    public required ImmutableDictionary<FieldId, FieldDefinition> FieldsById { get; init; }

    public required ImmutableDictionary<FieldGroupId, FieldGroupDefinition> FieldGroupsById { get; init; }

    public required ImmutableArray<string> OrderedFieldGroupIds { get; init; }

    public required ImmutableDictionary<FieldGroupId, int> FieldGroupIndexById { get; init; }

    public required ImmutableDictionary<FieldGroupId, string?> ParentFieldGroupById { get; init; }

    public required ImmutableArray<string> OrderedFieldIds { get; init; }

    public required ImmutableDictionary<FieldId, int> FieldIndexById { get; init; }

    public required ImmutableDictionary<FieldId, string> FieldToFieldGroupId { get; init; }

    public required ImmutableDictionary<FieldId, string> FieldToVariableId { get; init; }

    public required ImmutableDictionary<VariableId, EffectiveSensitiveDataPolicy> EffectivePoliciesByVariableId { get; init; }

    public required ImmutableArray<ConstraintDefinition> Constraints { get; init; }

    internal EffectiveSensitiveDataPolicy RequireEffectivePolicy(VariableId variableId)
    {
        if (!EffectivePoliciesByVariableId.TryGetValue(variableId, out var policy))
        {
            throw new InvalidOperationException("A resolved privacy policy was required for the requested variable and was not present.");
        }

        return policy;
    }

    /// <summary>
    /// Löst Felddefinition und gebundene Variablendefinition gemeinsam auf, damit
    /// Verfügbarkeitsprüfungen die Kennungskette nicht jeweils selbst zusammensetzen.
    /// </summary>
    internal (FieldDefinition Field, VariableDefinition Variable) ResolveFieldVariable(FieldId fieldId)
    {
        var field = FieldsById[fieldId];
        var variable = VariablesById[VariableId.From(FieldToVariableId[fieldId])];
        return (field, variable);
    }
}


