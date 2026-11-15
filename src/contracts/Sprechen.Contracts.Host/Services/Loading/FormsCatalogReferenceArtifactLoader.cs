using Json.Schema;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Contracts.Services.Loading;

/// <summary>
/// Ergänzt den neutralen Katalog um geprüfte Anwendungsdistributionen und prüft das Ergebnis, damit der ausgelieferte Katalog alle geladenen Anwendungen enthält.
/// </summary>
internal sealed class FormsCatalogReferenceArtifactLoader
{
    private readonly MdaArtifactReader _artifactReader;
    private readonly MdaReferenceJsonModelReader _modelReader;
    private readonly ApplicationDistributionReferenceArtifactLoader _distributionLoader;
    private readonly FormsCatalogComposer _composer;

    public FormsCatalogReferenceArtifactLoader(MdaArtifactReader artifactReader, MdaReferenceJsonModelReader modelReader)
        : this(artifactReader, modelReader, new ApplicationDistributionReferenceArtifactLoader(artifactReader), new FormsCatalogComposer())
    {
    }

    internal FormsCatalogReferenceArtifactLoader(
        MdaArtifactReader artifactReader,
        MdaReferenceJsonModelReader modelReader,
        ApplicationDistributionReferenceArtifactLoader distributionLoader,
        FormsCatalogComposer composer)
    {
        _artifactReader = artifactReader ?? throw new ArgumentNullException(nameof(artifactReader));
        _modelReader = modelReader ?? throw new ArgumentNullException(nameof(modelReader));
        _distributionLoader = distributionLoader ?? throw new ArgumentNullException(nameof(distributionLoader));
        _composer = composer ?? throw new ArgumentNullException(nameof(composer));
    }

    public FormsCatalogReferenceArtifact Load(JsonSchema catalogSchema, JsonSchema distributionSchema)
    {
        var neutral = _artifactReader.ReadJsonArtifact(MdaContractsArtifactPaths.CatalogFilePath, contentType: "application/json");
        var distributions = _distributionLoader.Load(distributionSchema);

        var composedBytes = _composer.Compose(neutral.Content, distributions);
        var artifact = _artifactReader.CreateJsonArtifact(composedBytes, contentType: "application/json", errorContext: "composed forms catalog");
        var document = _modelReader.Read<FormsCatalogDocument>(catalogSchema, artifact, MdaContractsArtifactPaths.CatalogFilePath);

        return new FormsCatalogReferenceArtifact(artifact, document);
    }
}
