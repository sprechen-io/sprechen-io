namespace Sprechen.Mda.Contracts.Generator;

/// <summary>
/// Ordnet den zu erzeugenden Vertragstypen Schema, Namensraum und Ausgabepfad zu und legt damit den Generierungsumfang fest.
/// </summary>
internal static class SchemaContractGenerationTargets
{
    internal static readonly (string SchemaFile, string? DefinitionName, string Namespace, string TypeName, string OutputPath)[] All =
    [
        (
            "application-selection-dialog.schema.json",
            null,
            "Sprechen.Mda.Contracts.V1.Catalog",
            "ApplicationSelectionDialog",
            Path.Combine("src", "mda", "Sprechen.Mda.Contracts", "V1", "Generated", "Catalog", "ApplicationSelectionDialog.cs")),
        (
            "application-selection-dialog.schema.json",
            "CatalogVoicePrompt",
            "Sprechen.Mda.Contracts.V1.Catalog",
            "CatalogVoicePrompt",
            Path.Combine("src", "mda", "Sprechen.Mda.Contracts", "V1", "Generated", "Catalog", "CatalogVoicePrompt.cs")),
        (
            "application-selection-dialog.schema.json",
            "CatalogVoiceIntent",
            "Sprechen.Mda.Contracts.V1.Catalog",
            "CatalogVoiceIntent",
            Path.Combine("src", "mda", "Sprechen.Mda.Contracts", "V1", "Generated", "Catalog", "CatalogVoiceIntent.cs")),
        (
            "application-selection-dialog.schema.json",
            "CatalogVoiceNumberWord",
            "Sprechen.Mda.Contracts.V1.Catalog",
            "CatalogVoiceNumberWord",
            Path.Combine("src", "mda", "Sprechen.Mda.Contracts", "V1", "Generated", "Catalog", "CatalogVoiceNumberWord.cs")),
        (
            "application-selection-dialog.schema.json",
            "VoiceDialogSettings",
            "Sprechen.Mda.Contracts.V1.Catalog",
            "VoiceDialogSettings",
            Path.Combine("src", "mda", "Sprechen.Mda.Contracts", "V1", "Generated", "Catalog", "VoiceDialogSettings.cs")),
        (
            "application-selection-dialog.schema.json",
            "FormDescriptor",
            "Sprechen.Mda.Contracts.V1.Catalog",
            "FormDescriptor",
            Path.Combine("src", "mda", "Sprechen.Mda.Contracts", "V1", "Generated", "Catalog", "FormDescriptor.cs")),
        (
            "application-selection-dialog.schema.json",
            "ApplicationDescriptor",
            "Sprechen.Mda.Contracts.V1.Catalog",
            "ApplicationDescriptor",
            Path.Combine("src", "mda", "Sprechen.Mda.Contracts", "V1", "Generated", "Catalog", "ApplicationDescriptor.cs")),
        (
            "application-selection-dialog-profile.schema.json",
            null,
            "Sprechen.Mda.Contracts.V1.Catalog",
            "ApplicationSelectionDialogProfile",
            Path.Combine("src", "mda", "Sprechen.Mda.Contracts", "V1", "Generated", "Catalog", "ApplicationSelectionDialogProfile.cs")),
        (
            "forms-catalog.schema.json",
            null,
            "Sprechen.Mda.Contracts.V1.Catalog",
            "FormsCatalogDocument",
            Path.Combine("src", "mda", "Sprechen.Mda.Contracts", "V1", "Generated", "Catalog", "FormsCatalogDocument.cs")),
        (
            "forms-catalog.schema.json",
            "ApplicationSelectionDialogReference",
            "Sprechen.Mda.Contracts.V1.Catalog",
            "ApplicationSelectionDialogReference",
            Path.Combine("src", "mda", "Sprechen.Mda.Contracts", "V1", "Generated", "Catalog", "ApplicationSelectionDialogReference.cs")),
        (
            "forms-catalog.schema.json",
            "ApplicationSelectionDialogProfileReference",
            "Sprechen.Mda.Contracts.V1.Catalog",
            "ApplicationSelectionDialogProfileReference",
            Path.Combine("src", "mda", "Sprechen.Mda.Contracts", "V1", "Generated", "Catalog", "ApplicationSelectionDialogProfileReference.cs")),
        (
            "forms-catalog.schema.json",
            "ApplicationDescriptorDocument",
            "Sprechen.Mda.Contracts.V1.Catalog",
            "ApplicationDescriptorDocument",
            Path.Combine("src", "mda", "Sprechen.Mda.Contracts", "V1", "Generated", "Catalog", "ApplicationDescriptorDocument.cs")),
        (
            "privacy-policy.schema.json",
            "SensitiveDataPolicy",
            "Sprechen.Mda.Contracts.V1.FormContracts",
            "SensitiveDataPolicy",
            Path.Combine("src", "mda", "Sprechen.Mda.Contracts", "V1", "Generated", "FormContracts", "SensitiveDataPolicy.cs")),
        (
            "form-contract.schema.json",
            null,
            "Sprechen.Mda.Contracts.V1.FormContracts",
            "FormContractDocument",
            Path.Combine("src", "mda", "Sprechen.Mda.Contracts", "V1", "Generated", "FormContracts", "FormContractDocument.cs")),
        (
            "form-contract.schema.json",
            "TraceInfo",
            "Sprechen.Mda.Contracts.V1.FormContracts",
            "TraceInfo",
            Path.Combine("src", "mda", "Sprechen.Mda.Contracts", "V1", "Generated", "FormContracts", "TraceInfo.cs")),
        (
            "form-contract.schema.json",
            "FieldDefinitionsReference",
            "Sprechen.Mda.Contracts.V1.FormContracts",
            "FieldDefinitionsReference",
            Path.Combine("src", "mda", "Sprechen.Mda.Contracts", "V1", "Generated", "FormContracts", "FieldDefinitionsReference.cs")),
        (
            "form-contract.schema.json",
            "FieldGroupDefinition",
            "Sprechen.Mda.Contracts.V1.FormContracts",
            "FieldGroupDefinition",
            Path.Combine("src", "mda", "Sprechen.Mda.Contracts", "V1", "Generated", "FormContracts", "FieldGroupDefinition.cs")),
        (
            "form-contract.schema.json",
            "FormFieldBindingDefinition",
            "Sprechen.Mda.Contracts.V1.FormContracts",
            "FormFieldBindingDefinition",
            Path.Combine("src", "mda", "Sprechen.Mda.Contracts", "V1", "Generated", "FormContracts", "FormFieldBindingDefinition.cs")),
        (
            "form-contract.schema.json",
            "FormVariableBindingDefinition",
            "Sprechen.Mda.Contracts.V1.FormContracts",
            "FormVariableBindingDefinition",
            Path.Combine("src", "mda", "Sprechen.Mda.Contracts", "V1", "Generated", "FormContracts", "FormVariableBindingDefinition.cs")),
        (
            "form-contract.schema.json",
            "ContractProjections",
            "Sprechen.Mda.Contracts.V1.FormContracts",
            "ContractProjectionsDocument",
            Path.Combine("src", "mda", "Sprechen.Mda.Contracts", "V1", "Generated", "FormContracts", "ContractProjectionsDocument.cs")),
        (
            "form-contract.schema.json",
            "VoiceProjection",
            "Sprechen.Mda.Contracts.V1.FormContracts",
            "VoiceProjectionDocument",
            Path.Combine("src", "mda", "Sprechen.Mda.Contracts", "V1", "Generated", "FormContracts", "VoiceProjectionDocument.cs")),
        (
            "form-contract.schema.json",
            "VoiceHintsDefinition",
            "Sprechen.Mda.Contracts.V1.FormContracts",
            "VoiceHintsDefinition",
            Path.Combine("src", "mda", "Sprechen.Mda.Contracts", "V1", "Generated", "FormContracts", "VoiceHintsDefinition.cs")),
        (
            "form-contract.schema.json",
            "VoicePrompt",
            "Sprechen.Mda.Contracts.V1.FormContracts",
            "VoicePrompt",
            Path.Combine("src", "mda", "Sprechen.Mda.Contracts", "V1", "Generated", "FormContracts", "VoicePrompt.cs")),
        (
            "form-contract.schema.json",
            "VoiceIntent",
            "Sprechen.Mda.Contracts.V1.FormContracts",
            "VoiceIntent",
            Path.Combine("src", "mda", "Sprechen.Mda.Contracts", "V1", "Generated", "FormContracts", "VoiceIntent.cs")),
        (
            "form-contract.schema.json",
            "SynonymSet",
            "Sprechen.Mda.Contracts.V1.FormContracts",
            "SynonymSet",
            Path.Combine("src", "mda", "Sprechen.Mda.Contracts", "V1", "Generated", "FormContracts", "SynonymSet.cs")),
        (
            "form-contract.schema.json",
            "GlobalCommandDefinition",
            "Sprechen.Mda.Contracts.V1.FormContracts",
            "GlobalCommandDefinition",
            Path.Combine("src", "mda", "Sprechen.Mda.Contracts", "V1", "Generated", "FormContracts", "GlobalCommandDefinition.cs")),
        (
            "form-contract.schema.json",
            "HtmlProjection",
            "Sprechen.Mda.Contracts.V1.FormContracts",
            "HtmlProjection",
            Path.Combine("src", "mda", "Sprechen.Mda.Contracts", "V1", "Generated", "FormContracts", "HtmlProjection.cs")),
        (
            "form-contract.schema.json",
            "RuleDefinition",
            "Sprechen.Mda.Contracts.V1.FormContracts",
            "RuleDefinition",
            Path.Combine("src", "mda", "Sprechen.Mda.Contracts", "V1", "Generated", "FormContracts", "RuleDefinition.cs")),
        (
            "field-definitions.schema.json",
            null,
            "Sprechen.Mda.Contracts.V1.Fields",
            "FieldDefinitions",
            Path.Combine("src", "mda", "Sprechen.Mda.Contracts", "V1", "Generated", "Fields", "FieldDefinitions.cs")),
        (
            "field-definitions.schema.json",
            "ReusableFieldDefinition",
            "Sprechen.Mda.Contracts.V1.Fields",
            "ReusableFieldDefinition",
            Path.Combine("src", "mda", "Sprechen.Mda.Contracts", "V1", "Generated", "Fields", "ReusableFieldDefinition.cs")),
        (
            "field-definitions.schema.json",
            "FieldProjections",
            "Sprechen.Mda.Contracts.V1.FormContracts",
            "FieldProjections",
            Path.Combine("src", "mda", "Sprechen.Mda.Contracts", "V1", "Generated", "FormContracts", "FieldProjections.cs")),
        (
            "field-definitions.schema.json",
            "FieldHtmlProjection",
            "Sprechen.Mda.Contracts.V1.FormContracts",
            "FieldHtmlProjection",
            Path.Combine("src", "mda", "Sprechen.Mda.Contracts", "V1", "Generated", "FormContracts", "FieldHtmlProjection.cs")),
        (
            "field-definitions.schema.json",
            "FieldVoiceProjection",
            "Sprechen.Mda.Contracts.V1.FormContracts",
            "FieldVoiceProjection",
            Path.Combine("src", "mda", "Sprechen.Mda.Contracts", "V1", "Generated", "FormContracts", "FieldVoiceProjection.cs")),
        (
            "field-definitions.schema.json",
            "FieldPresentationHints",
            "Sprechen.Mda.Contracts.V1.FormContracts",
            "FieldPresentationHints",
            Path.Combine("src", "mda", "Sprechen.Mda.Contracts", "V1", "Generated", "FormContracts", "FieldPresentationHints.cs")),
        (
            "form-contract.schema.json",
            "VariableDefinitionsReference",
            "Sprechen.Mda.Contracts.V1.Variables",
            "VariableDefinitionsReference",
            Path.Combine("src", "mda", "Sprechen.Mda.Contracts", "V1", "Generated", "Variables", "VariableDefinitionsReference.cs")),
        (
            "variable-definitions.schema.json",
            null,
            "Sprechen.Mda.Contracts.V1.Variables",
            "VariableDefinitions",
            Path.Combine("src", "mda", "Sprechen.Mda.Contracts", "V1", "Generated", "Variables", "VariableDefinitions.cs")),
        (
            "variable-definitions.schema.json",
            "ReusableVariableDefinition",
            "Sprechen.Mda.Contracts.V1.Variables",
            "ReusableVariableDefinition",
            Path.Combine("src", "mda", "Sprechen.Mda.Contracts", "V1", "Generated", "Variables", "ReusableVariableDefinition.cs")),
        (
            "status-announcement.schema.json",
            null,
            "Sprechen.Mda.Contracts.V1.Status",
            "StatusAnnouncement",
            Path.Combine("src", "mda", "Sprechen.Mda.Contracts", "V1", "Generated", "Status", "StatusAnnouncement.cs")),
        (
            "status-announcement.schema.json",
            "NavigationPosition",
            "Sprechen.Mda.Contracts.V1.Status",
            "NavigationPosition",
            Path.Combine("src", "mda", "Sprechen.Mda.Contracts", "V1", "Generated", "Status", "NavigationPosition.cs")),
        (
            "status-announcement.schema.json",
            "CompletionProgress",
            "Sprechen.Mda.Contracts.V1.Status",
            "CompletionProgress",
            Path.Combine("src", "mda", "Sprechen.Mda.Contracts", "V1", "Generated", "Status", "CompletionProgress.cs")),
        (
            "status-announcement.schema.json",
            "ProgressDimension",
            "Sprechen.Mda.Contracts.V1.Status",
            "ProgressDimension",
            Path.Combine("src", "mda", "Sprechen.Mda.Contracts", "V1", "Generated", "Status", "ProgressDimension.cs"))
    ];
}
