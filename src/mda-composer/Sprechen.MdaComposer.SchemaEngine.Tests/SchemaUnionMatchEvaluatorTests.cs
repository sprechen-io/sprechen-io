using System.Text.Json.Nodes;

namespace Sprechen.MdaComposer.SchemaEngine.Tests;

public sealed class SchemaUnionMatchEvaluatorTests
{
    private readonly SchemaUnionMatchEvaluator _evaluator = new();

    [Fact]
    public void HasMatchingOption_ReturnsTrue_WhenAnyVariantValidates()
    {
        var schema = CreateUnionSchema();

        Assert.True(_evaluator.HasMatchingOption(schema, JsonValue.Create("auto")!));
    }

    [Fact]
    public void HasMatchingOption_ReturnsFalse_WhenNoVariantValidates()
    {
        var schema = CreateUnionSchema();

        Assert.False(_evaluator.HasMatchingOption(schema, JsonValue.Create(true)!));
    }

    private static SchemaNode CreateUnionSchema()
    {
        return new SchemaNode
        {
            Name = "mode",
            Type = SchemaNodeType.OneOf,
            OneOf =
            [
                new SchemaNode { Name = "(option 0)", Type = SchemaNodeType.String, EnumValues = ["auto"] },
                new SchemaNode { Name = "(option 1)", Type = SchemaNodeType.Integer },
            ],
        };
    }
}
