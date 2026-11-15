using Json.Schema;
using Sprechen.Mda.Contracts.V1.Preflight;
using Sprechen.Mda.Contracts.V1.Variables;

namespace Sprechen.Contracts.Services.Loading;

/// <summary>
/// Lädt und prüft Variablendefinitionen mit Kennung und Version, damit ungültige oder doppelte Kataloge vor der Verwendung auffallen.
/// </summary>
internal sealed class VariableDefinitionsReferenceArtifactLoader
{
    private readonly MdaReferenceArtifactSetLoader _setLoader;

    public VariableDefinitionsReferenceArtifactLoader(MdaReferenceArtifactSetLoader setLoader)
    {
        _setLoader = setLoader ?? throw new ArgumentNullException(nameof(setLoader));
    }

    public ArtifactSet<VariableDefinitionsKey, VariableDefinitions> Load(JsonSchema schema)
    {
        return _setLoader.LoadCategory<VariableDefinitionsKey, VariableDefinitions>(
            MdaContractsArtifactPaths.VariableDefinitionsDirectoryPath,
            MdaContractsArtifactPaths.VariableDefinitionsCategoryName,
            schema,
            GetKey,
            key => $"Duplicate VariableDefinitions key (variableDefinitionsId='{key.VariableDefinitionsId}', catalogVersion='{key.CatalogVersion}').");
    }

    private static VariableDefinitionsKey GetKey(VariableDefinitions model)
    {
        var preflight = VariableDefinitionsPreflight.Validate(model);
        if (!preflight.Ok)
        {
            throw new InvalidOperationException(
                $"VariableDefinitions preflight failed ({model.VariableDefinitionsId}/{model.CatalogVersion}): {PreflightIssueFormatter.Format(preflight.Errors)}");
        }

        return MdaArtifactKeyFactory.CreateVariableDefinitionsKey(model.VariableDefinitionsId, model.CatalogVersion);
    }
}
