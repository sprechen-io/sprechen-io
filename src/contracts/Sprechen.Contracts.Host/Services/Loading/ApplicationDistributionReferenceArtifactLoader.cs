using System.Text.Json.Nodes;
using Json.Schema;

namespace Sprechen.Contracts.Services.Loading;

/// <summary>
/// Lädt Anwendungsdistributionen und prüft ihr Schema vor der Katalogzusammenstellung, damit ungültige Dokumente den Start abbrechen.
/// </summary>
internal sealed class ApplicationDistributionReferenceArtifactLoader
{
    private readonly MdaArtifactReader _artifactReader;
    private readonly MdaReferenceArtifactSchemaValidator _schemaValidator;

    public ApplicationDistributionReferenceArtifactLoader(MdaArtifactReader artifactReader)
        : this(artifactReader, new MdaReferenceArtifactSchemaValidator())
    {
    }

    internal ApplicationDistributionReferenceArtifactLoader(
        MdaArtifactReader artifactReader,
        MdaReferenceArtifactSchemaValidator schemaValidator)
    {
        _artifactReader = artifactReader ?? throw new ArgumentNullException(nameof(artifactReader));
        _schemaValidator = schemaValidator ?? throw new ArgumentNullException(nameof(schemaValidator));
    }

    public IReadOnlyList<ApplicationDistributionFragment> Load(JsonSchema distributionSchema)
    {
        ArgumentNullException.ThrowIfNull(distributionSchema);

        var distributions = new List<ApplicationDistributionFragment>();
        foreach (var directoryName in _artifactReader.EnumerateSubdirectories(MdaContractsArtifactPaths.ApplicationsRootDirectoryPath))
        {
            var path = $"{MdaContractsArtifactPaths.ApplicationsRootDirectoryPath}/{directoryName}/{MdaContractsArtifactPaths.ApplicationDistributionFileName}";
            var artifact = _artifactReader.ReadJsonArtifact(path, contentType: "application/json");
            _schemaValidator.Validate(distributionSchema, artifact, path);

            // Die vorherige Schemaprüfung stellt Objekttyp und Pflichtfelder sicher.
            var document = (JsonObject)JsonNode.Parse(artifact.Content)!;
            distributions.Add(new ApplicationDistributionFragment(
                document["distributionId"]!.GetValue<string>(),
                (JsonArray)document["applications"]!));
        }

        return distributions;
    }
}
