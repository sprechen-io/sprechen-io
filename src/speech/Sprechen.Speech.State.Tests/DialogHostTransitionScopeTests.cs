namespace Sprechen.Speech.State.Tests;

public sealed class DialogHostTransitionScopeTests
{
    [Fact]
    public void ThrowIfReentrant_WhenOutsideCallback_DoesNotThrow()
    {
        var scope = new DialogHostTransitionScope();

        scope.ThrowIfReentrant();
    }

    [Fact]
    public async Task RunAsync_WhenCallbackChecksReentrancy_Throws()
    {
        var scope = new DialogHostTransitionScope();

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await scope.RunAsync(() =>
            {
                scope.ThrowIfReentrant();
                return ValueTask.CompletedTask;
            });
        });
    }

    [Fact]
    public async Task RunAsync_WhenCallbackThrows_LeavesTransitionScope()
    {
        var scope = new DialogHostTransitionScope();

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await scope.RunAsync(() => throw new InvalidOperationException("boom"));
        });

        scope.ThrowIfReentrant();
    }
}
