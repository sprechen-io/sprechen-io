using Sprechen.Speech.State.Tests.TestDoubles;

namespace Sprechen.Speech.State.Tests;

public sealed class DialogHostModuleLifecycleTests
{
    [Fact]
    public async Task ActivateAsync_RunsCallbackInsideTransitionScope()
    {
        var transitionScope = new DialogHostTransitionScope();
        var lifecycle = new DialogHostModuleLifecycle<string>(transitionScope);
        var flow = new ReentrantDialogFlow<string>(() =>
        {
            transitionScope.ThrowIfReentrant();
            return ValueTask.CompletedTask;
        });

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await lifecycle.ActivateAsync(flow, CancellationToken.None));
    }

    [Fact]
    public async Task BestEffortDisposeAsync_WhenDisposeFails_DoesNotThrow()
    {
        var lifecycle = new DialogHostModuleLifecycle<string>(new DialogHostTransitionScope());
        var flow = new ControllableDialogFlow<string> { DisposeException = new InvalidOperationException("dispose") };

        await lifecycle.BestEffortDisposeAsync(flow);

        Assert.Equal(1, flow.DisposedCount);
    }
}
