using System.Text.Json;
using Json.Schema;
using Sprechen.Mda.SchemaBundle;

namespace Sprechen.Contracts.Services.Loading;

/// <summary>
/// Prüft JSON-Artefakte gegen ihr Schema, damit Aufrufer ungültige Dokumente ohne vorherige Deserialisierung in ein Fachmodell erkennen.
/// </summary>
internal sealed class MdaReferenceArtifactSchemaValidator
{
    private readonly JsonSchemaRefusalCollector _refusalCollector = new();

    public void Validate(JsonSchema schema, MdaJsonArtifact artifact, string errorContext)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorContext);

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(artifact.Content);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Invalid JSON ({errorContext}).", ex);
        }

        using (document)
        {
            var result = schema.Evaluate(document.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.Hierarchical });
            if (!result.IsValid)
            {
                throw new InvalidOperationException(
                    $"Schema validation failed ({errorContext}): {_refusalCollector.Describe(result)}");
            }
        }
    }
}
