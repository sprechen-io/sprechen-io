namespace Sprechen.Mda.Contracts.Generator;

/// <summary>
/// Bündelt Namens-, Typ- und Eigenschaftszuordnungen, damit Vertragsklassen nach einheitlichen Konventionen erzeugt werden.
/// </summary>
internal static class SchemaContractGenerationConventions
{
    internal static readonly IReadOnlyDictionary<string, string> KnownTypeNamespaces =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["LocalizedText"] = "Sprechen.Mda.Contracts.V1",
            ["FieldPresentationHints"] = "Sprechen.Mda.Contracts.V1.FormContracts",
            ["FieldProjections"] = "Sprechen.Mda.Contracts.V1.FormContracts",
            ["SensitiveDataPolicy"] = "Sprechen.Mda.Contracts.V1.FormContracts",
            ["ConstraintDefinition"] = "Sprechen.Mda.Contracts.V1.FormContracts",
            ["TraceInfo"] = "Sprechen.Mda.Contracts.V1.FormContracts",
            ["FormVoiceProfileReference"] = "Sprechen.Mda.Contracts.V1.FormVoice",
            ["VariableDefinitionsReference"] = "Sprechen.Mda.Contracts.V1.Variables"
        };

    internal static readonly IReadOnlyDictionary<string, string> KnownSchemaAliases =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["AnyNonNull"] = "JsonElement",
            ["Editability"] = "string",
            ["Id"] = "string",
            ["InputMode"] = "string",
            ["LanguageCode"] = "string",
            ["PromptId"] = "string",
            ["PrivacyClass"] = "string",
            ["ReadbackPolicy"] = "string",
            ["RuleScope"] = "string",
            ["SilentInputFallback"] = "string",
            ["SpeechRate"] = "double",
            ["ContractVersion"] = "string",
            ["CatalogVoiceIntentId"] = "string",
            ["CatalogVoiceIntentMatchMode"] = "string",
            ["ConfirmationStyle"] = "string",
            ["VoiceInputPolicy"] = "string"
        };

    internal static readonly IReadOnlyDictionary<string, string> KnownGeneratedTypeNames =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["ContractProjections"] = "ContractProjectionsDocument",
            ["VoiceProjection"] = "VoiceProjectionDocument"
        };

    internal static readonly IReadOnlySet<string> ForcedNamedReferenceTypes =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "ConstraintDefinition",
            "FieldGroupMember"
        };

    internal static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> PropertyNameOverrides =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal)
        {
            ["FormContractDocument"] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["fieldCatalog"] = "FieldDefinitions",
                ["variableCatalog"] = "VariableDefinitions"
            },
            ["TraceInfo"] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["rqIds"] = "RequestIds"
            }
        };

    internal static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, ResolvedPropertyType>> PropertyTypeOverrides =
        new Dictionary<string, IReadOnlyDictionary<string, ResolvedPropertyType>>(StringComparer.Ordinal)
        {
            ["FieldGroupDefinition"] = new Dictionary<string, ResolvedPropertyType>(StringComparer.Ordinal)
            {
                ["fieldGroupId"] = new("FieldGroupId", null, false, "Sprechen.Mda.Contracts.V1.Identifiers")
            },
            ["FormFieldBindingDefinition"] = new Dictionary<string, ResolvedPropertyType>(StringComparer.Ordinal)
            {
                ["fieldId"] = new("FieldId", null, false, "Sprechen.Mda.Contracts.V1.Identifiers"),
                ["variableId"] = new("VariableId", null, false, "Sprechen.Mda.Contracts.V1.Identifiers")
            },
            ["FormVariableBindingDefinition"] = new Dictionary<string, ResolvedPropertyType>(StringComparer.Ordinal)
            {
                ["variableId"] = new("VariableId", null, false, "Sprechen.Mda.Contracts.V1.Identifiers")
            },
            ["GlobalCommandDefinition"] = new Dictionary<string, ResolvedPropertyType>(StringComparer.Ordinal)
            {
                ["commandId"] = new("CommandId", null, false, "Sprechen.Mda.Contracts.V1.Identifiers")
            },
            ["FormContractDocument"] = new Dictionary<string, ResolvedPropertyType>(StringComparer.Ordinal)
            {
                ["formId"] = new("FormId", null, false, "Sprechen.Mda.Contracts.V1.Identifiers"),
                ["entryFieldGroupId"] = new("FieldGroupId", null, false, "Sprechen.Mda.Contracts.V1.Identifiers")
            },
            ["FormDescriptor"] = new Dictionary<string, ResolvedPropertyType>(StringComparer.Ordinal)
            {
                ["formId"] = new("FormId", null, false, "Sprechen.Mda.Contracts.V1.Identifiers")
            },
            ["ReusableFieldDefinition"] = new Dictionary<string, ResolvedPropertyType>(StringComparer.Ordinal)
            {
                ["editability"] = new("FieldEditability", null, false, "Sprechen.Mda.Contracts.V1.FormContracts")
            },
            ["ReusableVariableDefinition"] = new Dictionary<string, ResolvedPropertyType>(StringComparer.Ordinal)
            {
                ["privacyClass"] = new("PrivacyClass?", null, false, "Sprechen.Mda.Contracts.V1.FormContracts")
            }
        };
}
