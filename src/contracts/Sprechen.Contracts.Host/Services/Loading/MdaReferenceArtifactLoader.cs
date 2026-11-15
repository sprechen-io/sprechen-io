namespace Sprechen.Contracts.Services.Loading;

/// <summary>
/// Lädt die einzelnen Arten von Referenzartefakten gemeinsam, damit der Vertragsdienst einen zusammengehörigen Bestand prüfen kann.
/// </summary>
internal sealed class MdaReferenceArtifactLoader
{
    private readonly FieldDefinitionsReferenceArtifactLoader _fieldDefinitionsLoader;
    private readonly VariableDefinitionsReferenceArtifactLoader _variableDefinitionsLoader;
    private readonly FormVoiceProfileReferenceArtifactLoader _formVoiceProfileLoader;
    private readonly ApplicationSelectionDialogReferenceArtifactLoader _selectionDialogLoader;
    private readonly ApplicationSelectionDialogProfileReferenceArtifactLoader _selectionDialogProfileLoader;
    private readonly FormsCatalogReferenceArtifactLoader _formsCatalogLoader;
    private readonly FormContractReferenceArtifactLoader _formContractLoader;

    public MdaReferenceArtifactLoader(MdaArtifactReader artifactReader)
    {
        ArgumentNullException.ThrowIfNull(artifactReader);

        var modelReader = new MdaReferenceJsonModelReader();
        var setLoader = new MdaReferenceArtifactSetLoader(artifactReader, modelReader);
        _fieldDefinitionsLoader = new FieldDefinitionsReferenceArtifactLoader(setLoader);
        _variableDefinitionsLoader = new VariableDefinitionsReferenceArtifactLoader(setLoader);
        _formVoiceProfileLoader = new FormVoiceProfileReferenceArtifactLoader(setLoader);
        _selectionDialogLoader = new ApplicationSelectionDialogReferenceArtifactLoader(setLoader);
        _selectionDialogProfileLoader = new ApplicationSelectionDialogProfileReferenceArtifactLoader(setLoader);
        _formsCatalogLoader = new FormsCatalogReferenceArtifactLoader(artifactReader, modelReader);
        _formContractLoader = new FormContractReferenceArtifactLoader(setLoader);
    }

    public MdaReferenceArtifacts Load(MdaSchemaBundle schemas)
    {
        ArgumentNullException.ThrowIfNull(schemas);

        var formsCatalog = _formsCatalogLoader.Load(schemas.FormsCatalogSchema, schemas.ApplicationDistributionSchema);

        return new MdaReferenceArtifacts(
            _fieldDefinitionsLoader.Load(schemas.FieldDefinitionsSchema),
            _variableDefinitionsLoader.Load(schemas.VariableDefinitionsSchema),
            _formVoiceProfileLoader.Load(schemas.FormVoiceProfileSchema),
            _selectionDialogLoader.Load(schemas.ApplicationSelectionDialogSchema),
            _selectionDialogProfileLoader.Load(schemas.ApplicationSelectionDialogProfileSchema),
            formsCatalog.Artifact,
            formsCatalog.Document,
            _formContractLoader.Load(schemas.FormContractSchema));
    }
}
