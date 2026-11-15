using Sprechen.Mda.Contracts.V1.FormContracts;
namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Gemeinsam aufgebaute Kennungsverzeichnisse für die Formularvorprüfung,
/// damit einzelne Validatoren den Vertrag nicht erneut durchsuchen müssen.
/// </summary>
internal sealed record FormContractPreflightIndex(
    IReadOnlyDictionary<string, VariableDefinition> VariablesById,
    IReadOnlyDictionary<string, FieldDefinition> FieldsById,
    IReadOnlyDictionary<string, FieldGroupDefinition> FieldGroupsById,
    IReadOnlySet<string> RuleIds,
    IReadOnlySet<string> ConstraintIds,
    IReadOnlySet<string> PromptIds);
