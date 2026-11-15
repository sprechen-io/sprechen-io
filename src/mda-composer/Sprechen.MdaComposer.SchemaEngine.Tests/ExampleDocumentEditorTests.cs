using System.Text.Json.Nodes;

namespace Sprechen.MdaComposer.SchemaEngine.Tests;

public sealed class ExampleDocumentEditorTests
{
    [Fact]
    public void SetValue_ForRootPointer_ReplacesDocument()
    {
        var editor = new ExampleDocumentEditor();
        var replacement = JsonNode.Parse("""{"title":"new"}""");

        var result = editor.SetValue(JsonNode.Parse("""{"title":"old"}"""), JsonPointer.Root, replacement);

        Assert.Equal("""{"title":"new"}""", result?.ToJsonString());
    }

    [Fact]
    public void SetValue_ForEscapedObjectProperty_UpdatesProperty()
    {
        var editor = new ExampleDocumentEditor();
        var document = JsonNode.Parse("""{"a/b":{"~name":"old"}}""");

        var result = editor.SetValue(document, "/a~1b/~0name", JsonValue.Create("new"));

        Assert.Equal("new", result?["a/b"]?["~name"]?.ToString());
    }

    [Fact]
    public void SetValue_ForArrayIndex_UpdatesItem()
    {
        var editor = new ExampleDocumentEditor();
        var document = JsonNode.Parse("""{"items":["old","keep"]}""");

        var result = editor.SetValue(document, "/items/0", JsonValue.Create("new"));

        Assert.Equal("new", result?["items"]?[0]?.ToString());
        Assert.Equal("keep", result?["items"]?[1]?.ToString());
    }

    [Fact]
    public void SetValue_WhenParentPointerIsMissing_LeavesDocumentUnchanged()
    {
        var editor = new ExampleDocumentEditor();
        var document = JsonNode.Parse("""{"title":"old"}""");

        var result = editor.SetValue(document, "/missing/title", JsonValue.Create("new"));

        Assert.Equal("""{"title":"old"}""", result?.ToJsonString());
    }
}
