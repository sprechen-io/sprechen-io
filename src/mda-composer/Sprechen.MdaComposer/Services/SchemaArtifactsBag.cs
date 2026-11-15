using Sprechen.MdaComposer.SchemaEngine;

namespace Sprechen.MdaComposer.Services;

/// <summary>
/// Hält Schemainhalte, aufbereitete Schemaknoten und Referenzverträge für den gemeinsamen Zugriff im Composer bereit.
/// </summary>
internal sealed class SchemaArtifactsBag
{
    private readonly Dictionary<string, SchemaNode> _parsedSchemas = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _rawSchemas = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ReferenceContract> _referenceContracts = [];

    public IReadOnlyDictionary<string, SchemaNode> ParsedSchemas => _parsedSchemas;
    public IReadOnlyDictionary<string, string> RawSchemas => _rawSchemas;
    public IReadOnlyList<ReferenceContract> ReferenceContracts => _referenceContracts;

    public void StoreRawSchema(string schemaFile, string json) => _rawSchemas[schemaFile] = json;

    public void StoreParsedSchema(string schemaFile, SchemaNode node) => _parsedSchemas[schemaFile] = node;

    public void AddReferenceContract(ReferenceContract contract) => _referenceContracts.Add(contract);

    public void Clear()
    {
        _parsedSchemas.Clear();
        _rawSchemas.Clear();
        _referenceContracts.Clear();
    }
}
