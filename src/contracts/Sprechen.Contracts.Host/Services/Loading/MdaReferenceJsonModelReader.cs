using System.Text.Json;
using Json.Schema;

namespace Sprechen.Contracts.Services.Loading;

/// <summary>
/// Prüft JSON-Artefakte vor der Deserialisierung gegen ihr Schema, damit nur gültige Dokumente als Fachmodelle zurückgegeben werden.
/// </summary>
internal sealed class MdaReferenceJsonModelReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly MdaReferenceArtifactSchemaValidator _schemaValidator;

    public MdaReferenceJsonModelReader()
        : this(new MdaReferenceArtifactSchemaValidator())
    {
    }

    internal MdaReferenceJsonModelReader(MdaReferenceArtifactSchemaValidator schemaValidator)
    {
        _schemaValidator = schemaValidator ?? throw new ArgumentNullException(nameof(schemaValidator));
    }

    public T Read<T>(JsonSchema schema, MdaJsonArtifact artifact, string errorContext)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorContext);

        _schemaValidator.Validate(schema, artifact, errorContext);

        try
        {
            var model = JsonSerializer.Deserialize<T>(artifact.Content, JsonOptions);
            return model ?? throw new InvalidOperationException($"JSON deserialized to null ({errorContext}).");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Invalid JSON ({errorContext}).", ex);
        }
    }
}
