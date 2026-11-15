namespace Sprechen.MdaComposer.SchemaEngine.Tests;

public sealed class SchemaNodeChildTraversalTests
{
    [Fact]
    public void ReadChildren_ReturnsAllSchemaNodeChildSurfaces()
    {
        var traversal = new SchemaNodeChildTraversal();
        var schema = new SchemaNode
        {
            Name = "(root)",
            Type = SchemaNodeType.Object,
            Properties = [new SchemaNode { Name = "property", Type = SchemaNodeType.String }],
            Items = new SchemaNode { Name = "(items)", Type = SchemaNodeType.String },
            Definitions = new Dictionary<string, SchemaNode>
            {
                ["DefinedValue"] = new SchemaNode { Name = "DefinedValue", Type = SchemaNodeType.String },
            },
            OneOf = [new SchemaNode { Name = "(option 0)", Type = SchemaNodeType.String }],
            AnyOf = [new SchemaNode { Name = "(option 1)", Type = SchemaNodeType.Integer }],
        };

        var children = traversal.ReadChildren(schema, "(root)").ToList();

        Assert.Equal(
            ["property", "(items)", "DefinedValue", "(option 0)", "(option 1)"],
            children.Select(child => child.Node.Name));
        Assert.Contains(children, child => child.ParentPath == "$defs.DefinedValue");
    }
}
