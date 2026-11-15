using System.Text.Json.Nodes;

namespace Sprechen.MdaComposer.SchemaEngine.Tests;

public sealed class ExampleDocumentArrayEditorTests
{
    [Fact]
    public void TrySetItem_WhenSegmentIsValid_UpdatesArrayItem()
    {
        var editor = new ExampleDocumentArrayEditor();
        var array = new JsonArray("old", "keep");

        var changed = editor.TrySetItem(array, "0", JsonValue.Create("new"));

        Assert.True(changed);
        Assert.Equal("new", array[0]?.ToString());
        Assert.Equal("keep", array[1]?.ToString());
    }

    [Fact]
    public void TrySetItem_WhenSegmentIsInvalid_LeavesArrayUnchanged()
    {
        var editor = new ExampleDocumentArrayEditor();
        var array = new JsonArray("old");

        var changed = editor.TrySetItem(array, "x", JsonValue.Create("new"));

        Assert.False(changed);
        Assert.Equal("old", array[0]?.ToString());
    }

    [Fact]
    public void MoveItem_WhenTargetIndexIsValid_ReordersArray()
    {
        var editor = new ExampleDocumentArrayEditor();
        var array = new JsonArray("first", "second", "third");

        var changed = editor.MoveItem(array, 0, 1);

        Assert.True(changed);
        Assert.Equal("second", array[0]?.ToString());
        Assert.Equal("first", array[1]?.ToString());
        Assert.Equal("third", array[2]?.ToString());
    }
}
