using Json.Schema;
using Sprechen.Mda.Contracts.V1.Fields;
using Sprechen.Mda.Contracts.V1.Preflight;

namespace Sprechen.Contracts.Services.Loading;

/// <summary>
/// Lädt und prüft Felddefinitionen mit Kennung und Katalogversion, damit ungültige oder doppelte Definitionen den Start abbrechen.
/// </summary>
internal sealed class FieldDefinitionsReferenceArtifactLoader
{
    private readonly MdaReferenceArtifactSetLoader _setLoader;

    public FieldDefinitionsReferenceArtifactLoader(MdaReferenceArtifactSetLoader setLoader)
    {
        _setLoader = setLoader ?? throw new ArgumentNullException(nameof(setLoader));
    }

    public ArtifactSet<FieldDefinitionsKey, FieldDefinitions> Load(JsonSchema schema)
    {
        return _setLoader.LoadCategory<FieldDefinitionsKey, FieldDefinitions>(
            MdaContractsArtifactPaths.FieldDefinitionsDirectoryPath,
            MdaContractsArtifactPaths.FieldDefinitionsCategoryName,
            schema,
            GetKey,
            key => $"Duplicate FieldDefinitions key (fieldDefinitionsId='{key.FieldDefinitionsId}', catalogVersion='{key.CatalogVersion}').");
    }

    private static FieldDefinitionsKey GetKey(FieldDefinitions model)
    {
        var preflight = FieldDefinitionsPreflight.Validate(model);
        if (!preflight.Ok)
        {
            throw new InvalidOperationException(
                $"FieldDefinitions preflight failed ({model.FieldDefinitionsId}/{model.CatalogVersion}): {PreflightIssueFormatter.Format(preflight.Errors)}");
        }

        return MdaArtifactKeyFactory.CreateFieldDefinitionsKey(model.FieldDefinitionsId, model.CatalogVersion);
    }
}
