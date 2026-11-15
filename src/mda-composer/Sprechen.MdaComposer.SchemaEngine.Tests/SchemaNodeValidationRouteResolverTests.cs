namespace Sprechen.MdaComposer.SchemaEngine.Tests;

public sealed class SchemaNodeValidationRouteResolverTests
{
    private readonly SchemaNodeValidationRouteResolver _resolver = new();

    [Theory]
    [InlineData(SchemaNodeType.Array, false, (int)SchemaNodeValidationRoute.Array)]
    [InlineData(SchemaNodeType.String, false, (int)SchemaNodeValidationRoute.String)]
    [InlineData(SchemaNodeType.Integer, false, (int)SchemaNodeValidationRoute.Integer)]
    [InlineData(SchemaNodeType.Number, false, (int)SchemaNodeValidationRoute.Number)]
    [InlineData(SchemaNodeType.Boolean, false, (int)SchemaNodeValidationRoute.Boolean)]
    [InlineData(SchemaNodeType.OneOf, false, (int)SchemaNodeValidationRoute.Union)]
    [InlineData(SchemaNodeType.AnyOf, false, (int)SchemaNodeValidationRoute.Union)]
    [InlineData(SchemaNodeType.Unknown, false, (int)SchemaNodeValidationRoute.Ignored)]
    public void Resolve_RoutesSchemaNodeTypes_ToValidationOwners(
        SchemaNodeType type,
        bool isMap,
        int expectedRoute)
    {
        var schema = new SchemaNode { Name = "node", Type = type, IsMap = isMap };

        var route = _resolver.Resolve(schema);

        Assert.Equal((SchemaNodeValidationRoute)expectedRoute, route);
    }

    [Theory]
    [InlineData(false, (int)SchemaNodeValidationRoute.Object)]
    [InlineData(true, (int)SchemaNodeValidationRoute.Map)]
    public void Resolve_SplitsObjectAndMapValidationRoutes(bool isMap, int expectedRoute)
    {
        var schema = new SchemaNode { Name = "node", Type = SchemaNodeType.Object, IsMap = isMap };

        var route = _resolver.Resolve(schema);

        Assert.Equal((SchemaNodeValidationRoute)expectedRoute, route);
    }
}
