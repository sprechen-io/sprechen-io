using System.Text.Json.Nodes;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Wählt die passende Erzeugung von Anfangswerten nach Schematyp, damit verschachtelte Entwürfe einheitlich initialisiert werden.
/// </summary>
internal sealed class SchemaDefaultValueFactory
{
    private readonly SchemaDefaultObjectFactory _objectFactory = new();
    private readonly SchemaDefaultScalarValueFactory _scalarFactory = new();
    private readonly SchemaDefaultUnionFactory _unionFactory = new();
    private readonly IReadOnlyDictionary<SchemaNodeType, Func<SchemaNode, JsonNode?>> _factories;

    public SchemaDefaultValueFactory()
    {
        _factories = new Dictionary<SchemaNodeType, Func<SchemaNode, JsonNode?>>
        {
            [SchemaNodeType.Object] = schema => _objectFactory.Create(schema, Create),
            [SchemaNodeType.Array] = _ => new JsonArray(),
            [SchemaNodeType.String] = _scalarFactory.CreateString,
            [SchemaNodeType.Number] = _scalarFactory.CreateNumber,
            [SchemaNodeType.Integer] = _scalarFactory.CreateInteger,
            [SchemaNodeType.Boolean] = _scalarFactory.CreateBoolean,
            [SchemaNodeType.OneOf] = schema => _unionFactory.Create(schema, Create),
            [SchemaNodeType.AnyOf] = schema => _unionFactory.Create(schema, Create),
        };
    }

    public JsonNode? Create(SchemaNode schema)
    {
        return _factories.TryGetValue(schema.Type, out var factory)
            ? factory(schema)
            : _scalarFactory.CreateUnknown();
    }
}
