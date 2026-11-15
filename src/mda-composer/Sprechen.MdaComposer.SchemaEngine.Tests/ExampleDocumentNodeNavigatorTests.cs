using System.Text.Json.Nodes;

namespace Sprechen.MdaComposer.SchemaEngine.Tests;

public sealed class ExampleDocumentNodeNavigatorTests
{
    [Fact]
    public void GetNode_Resolves_ObjectAndArrayPointerSegments()
    {
        var document = JsonNode.Parse("""{"items":[{"name":"first"}]}""");
        var navigator = new ExampleDocumentNodeNavigator();

        var node = navigator.GetNode(document, "/items/0/name");

        Assert.Equal("first", node?.ToString());
    }

    [Fact]
    public void GetNode_WhenArraySegmentIsInvalid_ReturnsNull()
    {
        var document = JsonNode.Parse("""{"items":[{"name":"first"}]}""");
        var navigator = new ExampleDocumentNodeNavigator();

        Assert.Null(navigator.GetNode(document, "/items/x/name"));
        Assert.Null(navigator.GetNode(document, "/items/9/name"));
    }

    [Fact]
    public void GetNode_WhenPointerTraversesScalar_ReturnsNull()
    {
        var document = JsonNode.Parse("""{"title":"value"}""");
        var navigator = new ExampleDocumentNodeNavigator();

        Assert.Null(navigator.GetNode(document, "/title/name"));
    }
}
