using System.Text.Json.Nodes;

namespace Sprechen.MdaComposer.SchemaEngine.Tests;

public sealed class ExampleRepairCommandApplierTests
{
    [Theory]
    [InlineData(ExampleRepairActionKind.InitializeMissingRequiredValue)]
    [InlineData(ExampleRepairActionKind.ResetNodeToSchemaDefault)]
    [InlineData(ExampleRepairActionKind.ResetUnionToDefaultVariant)]
    public void Apply_ForResetRepairKinds_SetsTargetNodeToSchemaDefault(ExampleRepairActionKind kind)
    {
        var document = JsonNode.Parse("""{"title":"old"}""");
        var applier = new ExampleRepairCommandApplier();

        var result = applier.Apply(document, new ExampleRepairAction(
            "repair",
            kind,
            "/title",
            "Repair title",
            "Repair description",
            new SchemaNode
            {
                Name = "title",
                Type = SchemaNodeType.String,
            }));

        Assert.Equal(string.Empty, result?["title"]?.ToString());
    }

    [Fact]
    public void Apply_ForUnsupportedRepairKind_LeavesDocumentUnchanged()
    {
        var document = JsonNode.Parse("""{"title":"old"}""");
        var applier = new ExampleRepairCommandApplier();

        var result = applier.Apply(document, new ExampleRepairAction(
            "repair",
            (ExampleRepairActionKind)999,
            "/title",
            "Repair title",
            "Repair description",
            new SchemaNode
            {
                Name = "title",
                Type = SchemaNodeType.String,
            }));

        Assert.Equal("old", result?["title"]?.ToString());
    }
}
