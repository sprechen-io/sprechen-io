namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Ordnet Schemaknoten dem passenden Validator zu und unterscheidet dabei Objekte mit festen Eigenschaften von Maps.
/// </summary>
internal sealed class SchemaNodeValidationRouteResolver
{
    private readonly IReadOnlyDictionary<SchemaNodeType, SchemaNodeValidationRoute> _routes =
        new Dictionary<SchemaNodeType, SchemaNodeValidationRoute>
        {
            [SchemaNodeType.Array] = SchemaNodeValidationRoute.Array,
            [SchemaNodeType.String] = SchemaNodeValidationRoute.String,
            [SchemaNodeType.Integer] = SchemaNodeValidationRoute.Integer,
            [SchemaNodeType.Number] = SchemaNodeValidationRoute.Number,
            [SchemaNodeType.Boolean] = SchemaNodeValidationRoute.Boolean,
            [SchemaNodeType.OneOf] = SchemaNodeValidationRoute.Union,
            [SchemaNodeType.AnyOf] = SchemaNodeValidationRoute.Union,
        };

    public SchemaNodeValidationRoute Resolve(SchemaNode schema)
    {
        if (schema.Type == SchemaNodeType.Object)
        {
            return schema.IsMap ? SchemaNodeValidationRoute.Map : SchemaNodeValidationRoute.Object;
        }

        return _routes.TryGetValue(schema.Type, out var route) ? route : SchemaNodeValidationRoute.Ignored;
    }
}
