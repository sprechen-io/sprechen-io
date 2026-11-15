using Sprechen.Speech.State.Tests.TestDoubles;

namespace Sprechen.Speech.State.Tests;

public sealed class DialogHostStackTests
{
    [Fact]
    public void AddAndRemoveTop_PublishDepthAndActiveModule()
    {
        var stack = new DialogHostStack<string>();
        var root = new RecordingDialogFlow<string>();
        var modal = new RecordingDialogFlow<string>();

        stack.Add(root);
        stack.Add(modal);

        Assert.Equal(2, stack.Depth);
        Assert.Same(modal, stack.ActiveOrThrow());
        Assert.Same(root, stack.PreviousActiveOrDefault());

        var removed = stack.RemoveTop();

        Assert.Same(modal, removed);
        Assert.Equal(1, stack.Depth);
        Assert.Same(root, stack.ActiveOrThrow());
    }

    [Fact]
    public void SnapshotAndContainsReference_UseCurrentReferenceState()
    {
        var stack = new DialogHostStack<string>();
        var root = new RecordingDialogFlow<string>();
        var modal = new RecordingDialogFlow<string>();

        stack.Add(root);
        stack.Add(modal);
        var snapshot = stack.Snapshot();
        stack.RemoveTop();

        Assert.Equal([root, modal], snapshot);
        Assert.True(stack.ContainsReference(root));
        Assert.False(stack.ContainsReference(modal));
    }

    [Fact]
    public void ActiveOrThrow_WhenEmpty_Throws()
    {
        var stack = new DialogHostStack<string>();

        Assert.Throws<InvalidOperationException>(stack.ActiveOrThrow);
    }

    [Fact]
    public void ReplaceTop_WhenEmpty_Throws()
    {
        var stack = new DialogHostStack<string>();

        Assert.Throws<InvalidOperationException>(() => stack.ReplaceTop(new RecordingDialogFlow<string>()));
    }

    [Fact]
    public void Clear_PublishesEmptyDepth()
    {
        var stack = new DialogHostStack<string>();
        stack.Add(new RecordingDialogFlow<string>());

        stack.Clear();

        Assert.Equal(0, stack.Depth);
        Assert.True(stack.IsEmpty);
        Assert.Null(stack.ActiveOrDefault());
    }
}
