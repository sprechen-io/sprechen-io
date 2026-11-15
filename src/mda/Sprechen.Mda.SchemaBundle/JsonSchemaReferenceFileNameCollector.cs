using System.Text.Json;

namespace Sprechen.Mda.SchemaBundle;

/// <summary>
/// Sammelt Dateinamen aus <c>$ref</c>-Zeichenketten im JSON-Dokument
/// und fasst gleiche Namen ohne Beachtung der Groß- und Kleinschreibung zusammen.
/// </summary>
public sealed class JsonSchemaReferenceFileNameCollector
{
    private readonly JsonSchemaReferenceFileNameResolver _referenceFileNameResolver;

    public JsonSchemaReferenceFileNameCollector()
        : this(new JsonSchemaReferenceFileNameResolver())
    {
    }

    public JsonSchemaReferenceFileNameCollector(JsonSchemaReferenceFileNameResolver referenceFileNameResolver)
    {
        _referenceFileNameResolver = referenceFileNameResolver ?? throw new ArgumentNullException(nameof(referenceFileNameResolver));
    }

    public IReadOnlySet<string> Collect(string schemaText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaText);

        using var document = JsonDocument.Parse(schemaText);
        var fileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Collect(document.RootElement, fileNames);
        return fileNames;
    }

    private void Collect(JsonElement element, ISet<string> fileNames)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                CollectObject(element, fileNames);
                break;
            case JsonValueKind.Array:
                CollectArray(element, fileNames);
                break;
        }
    }

    private void CollectObject(JsonElement element, ISet<string> fileNames)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (property.NameEquals("$ref") && property.Value.ValueKind == JsonValueKind.String)
            {
                var fileName = _referenceFileNameResolver.Resolve(property.Value.GetString());
                if (fileName is not null)
                {
                    fileNames.Add(fileName);
                }

                continue;
            }

            Collect(property.Value, fileNames);
        }
    }

    private void CollectArray(JsonElement element, ISet<string> fileNames)
    {
        foreach (var item in element.EnumerateArray())
        {
            Collect(item, fileNames);
        }
    }
}
