namespace Sprechen.Contracts.Services.Loading;

/// <summary>
/// Prüft die Verweise zwischen geladenen Modellartefakten, damit fehlende und unreferenzierte Bestandteile vor der Auslieferung auffallen.
/// </summary>
internal sealed class MdaContractsBundleConsistencyValidator
{
    private readonly ApplicationSelectionDialogBundleConsistencyValidator _applicationSelectionDialogValidator;
    private readonly FormsCatalogContractReferenceCollector _contractReferenceCollector;
    private readonly FormContractBundleConsistencyValidator _formContractValidator;
    private readonly MdaReferenceArtifactPresenceValidator _presenceValidator;
    private readonly MdaContractsBundleAssembler _bundleAssembler;

    public MdaContractsBundleConsistencyValidator()
        : this(
            new ApplicationSelectionDialogBundleConsistencyValidator(),
            new FormsCatalogContractReferenceCollector(),
            new FormContractBundleConsistencyValidator(),
            new MdaReferenceArtifactPresenceValidator(),
            new MdaContractsBundleAssembler())
    {
    }

    internal MdaContractsBundleConsistencyValidator(
        ApplicationSelectionDialogBundleConsistencyValidator applicationSelectionDialogValidator,
        FormsCatalogContractReferenceCollector contractReferenceCollector,
        FormContractBundleConsistencyValidator formContractValidator,
        MdaReferenceArtifactPresenceValidator presenceValidator,
        MdaContractsBundleAssembler bundleAssembler)
    {
        _applicationSelectionDialogValidator = applicationSelectionDialogValidator ?? throw new ArgumentNullException(nameof(applicationSelectionDialogValidator));
        _contractReferenceCollector = contractReferenceCollector ?? throw new ArgumentNullException(nameof(contractReferenceCollector));
        _formContractValidator = formContractValidator ?? throw new ArgumentNullException(nameof(formContractValidator));
        _presenceValidator = presenceValidator ?? throw new ArgumentNullException(nameof(presenceValidator));
        _bundleAssembler = bundleAssembler ?? throw new ArgumentNullException(nameof(bundleAssembler));
    }

    public MdaContractsBundle Validate(MdaSchemaBundle schemas, MdaReferenceArtifacts references)
    {
        ArgumentNullException.ThrowIfNull(schemas);
        ArgumentNullException.ThrowIfNull(references);

        var catalogDocument = references.FormsCatalogDocument;
        var selectionDialogResult = _applicationSelectionDialogValidator.Validate(catalogDocument, references);
        var referencedContractKeys = _contractReferenceCollector.Collect(catalogDocument);
        var formContractResult = _formContractValidator.Validate(referencedContractKeys, references);

        _presenceValidator.EnsureReferencedContractsExist(referencedContractKeys, references.FormContracts.Artifacts);
        _presenceValidator.EnsureReferencedArtifactsExist(
            formContractResult.ReferencedFieldDefinitionsKeys,
            references.FieldDefinitions.Artifacts,
            "FieldDefinitions",
            key => $"fieldDefinitionsId='{key.FieldDefinitionsId}', catalogVersion='{key.CatalogVersion}'");
        _presenceValidator.EnsureReferencedArtifactsExist(
            formContractResult.ReferencedVariableDefinitionsKeys,
            references.VariableDefinitions.Artifacts,
            "VariableDefinitions",
            key => $"variableDefinitionsId='{key.VariableDefinitionsId}', catalogVersion='{key.CatalogVersion}'");
        _presenceValidator.EnsureReferencedArtifactsExist(
            formContractResult.ReferencedFormVoiceProfileKeys,
            references.FormVoiceProfiles.Artifacts,
            "FormVoiceProfile",
            key => $"formVoiceProfileId='{key.FormVoiceProfileId}', profileVersion='{key.ProfileVersion}'");
        _presenceValidator.EnsureNoUnreferencedArtifacts(
            references.FieldDefinitions.Artifacts.Keys,
            formContractResult.ReferencedFieldDefinitionsKeys,
            "FieldDefinitions",
            key => $"fieldDefinitionsId='{key.FieldDefinitionsId}', catalogVersion='{key.CatalogVersion}'");
        _presenceValidator.EnsureNoUnreferencedArtifacts(
            references.VariableDefinitions.Artifacts.Keys,
            formContractResult.ReferencedVariableDefinitionsKeys,
            "VariableDefinitions",
            key => $"variableDefinitionsId='{key.VariableDefinitionsId}', catalogVersion='{key.CatalogVersion}'");
        _presenceValidator.EnsureNoUnreferencedArtifacts(
            references.FormVoiceProfiles.Artifacts.Keys,
            formContractResult.ReferencedFormVoiceProfileKeys,
            "FormVoiceProfile",
            key => $"formVoiceProfileId='{key.FormVoiceProfileId}', profileVersion='{key.ProfileVersion}'");
        _presenceValidator.EnsureNoUnreferencedArtifacts(
            references.SelectionDialogs.Artifacts.Keys,
            [selectionDialogResult.SelectionDialogKey],
            "ApplicationSelectionDialog",
            key => $"selectionDialogId='{key.SelectionDialogId}', modelVersion='{key.ModelVersion}'");
        _presenceValidator.EnsureNoUnreferencedArtifacts(
            references.SelectionDialogProfiles.Artifacts.Keys,
            selectionDialogResult.ReferencedProfileKeys,
            "ApplicationSelectionDialogProfile",
            key => $"profileId='{key.ProfileId}', profileVersion='{key.ProfileVersion}'");

        return _bundleAssembler.Assemble(schemas, references);
    }
}
