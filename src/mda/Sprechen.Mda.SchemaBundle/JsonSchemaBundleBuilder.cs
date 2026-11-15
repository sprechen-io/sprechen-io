using System.Text;
using Json.Schema;

namespace Sprechen.Mda.SchemaBundle;

/// <summary>
/// Baut JSON-Schemata mit gemeinsamem Register in der Reihenfolge ihrer
/// Dateiabhängigkeiten auf und meldet fehlende Dateien oder zyklische Abhängigkeiten.
/// </summary>
public sealed class JsonSchemaBundleBuilder
{
    private readonly JsonSchemaReferenceFileNameCollector _referenceFileNameCollector;

    public JsonSchemaBundleBuilder()
        : this(new JsonSchemaReferenceFileNameCollector())
    {
    }

    public JsonSchemaBundleBuilder(JsonSchemaReferenceFileNameCollector referenceFileNameCollector)
    {
        _referenceFileNameCollector = referenceFileNameCollector ?? throw new ArgumentNullException(nameof(referenceFileNameCollector));
    }

    public IReadOnlyDictionary<string, JsonSchema> Build(IReadOnlyDictionary<string, byte[]> schemaContentsByFileName)
    {
        ArgumentNullException.ThrowIfNull(schemaContentsByFileName);

        var schemaTextsByFileName = schemaContentsByFileName.ToDictionary(
            static pair => pair.Key,
            static pair => Encoding.UTF8.GetString(pair.Value),
            StringComparer.OrdinalIgnoreCase);
        return Build(schemaTextsByFileName);
    }

    public IReadOnlyDictionary<string, JsonSchema> Build(IReadOnlyDictionary<string, string> schemaTextsByFileName)
    {
        ArgumentNullException.ThrowIfNull(schemaTextsByFileName);

        var registry = new SchemaRegistry();
        var builtSchemas = new Dictionary<string, JsonSchema>(StringComparer.OrdinalIgnoreCase);
        var building = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var schemaFileName in schemaTextsByFileName.Keys.OrderBy(static name => name, StringComparer.OrdinalIgnoreCase))
        {
            BuildSchemaRecursive(schemaFileName, schemaTextsByFileName, builtSchemas, building, registry);
        }

        return builtSchemas;
    }

    private JsonSchema BuildSchemaRecursive(
        string schemaFileName,
        IReadOnlyDictionary<string, string> schemaTextsByFileName,
        IDictionary<string, JsonSchema> builtSchemas,
        ISet<string> building,
        SchemaRegistry registry)
    {
        if (builtSchemas.TryGetValue(schemaFileName, out var existingSchema))
        {
            return existingSchema;
        }

        if (!schemaTextsByFileName.TryGetValue(schemaFileName, out var schemaText))
        {
            throw new InvalidOperationException($"Missing schema text for '{schemaFileName}'.");
        }

        if (!building.Add(schemaFileName))
        {
            throw new InvalidOperationException($"Detected cyclic schema bundle dependency while building '{schemaFileName}'.");
        }

        try
        {
            foreach (var dependencyFileName in _referenceFileNameCollector.Collect(schemaText))
            {
                BuildSchemaRecursive(dependencyFileName, schemaTextsByFileName, builtSchemas, building, registry);
            }

            var schema = JsonSchema.FromText(
                schemaText,
                new BuildOptions
                {
                    SchemaRegistry = registry
                });
            builtSchemas.Add(schemaFileName, schema);
            return schema;
        }
        finally
        {
            building.Remove(schemaFileName);
        }
    }
}
