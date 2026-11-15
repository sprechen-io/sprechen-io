using Sprechen.Speech.State;
using Sprechen.Speech.State.Tests.TestDoubles;

namespace Sprechen.Speech.State.Tests;

public sealed class DialogHostTests
{
    [Fact]
    public async Task StartAsync_ActivatesRoot()
    {
        await using var host = new DialogHost<string>();
        var root = new RecordingDialogFlow<string>();

        await host.StartAsync(root);

        Assert.Equal(1, host.Depth);
        Assert.Equal(1, root.ActivatedCount);
        Assert.Equal(0, root.DeactivatedCount);
    }

    [Fact]
    public async Task PushAsync_DeactivatesPrevious_ActivatesNew()
    {
        await using var host = new DialogHost<string>();
        var root = new RecordingDialogFlow<string>();
        await host.StartAsync(root);

        var modal = new RecordingDialogFlow<string>();
        await host.PushAsync(modal);

        Assert.Equal(2, host.Depth);
        Assert.Equal(1, root.DeactivatedCount);
        Assert.Equal(1, modal.ActivatedCount);
    }

    [Fact]
    public async Task DispatchToActiveAsync_SendsOnlyToTopModule()
    {
        await using var host = new DialogHost<string>();
        var root = new RecordingDialogFlow<string>();
        await host.StartAsync(root);

        var modal = new RecordingDialogFlow<string>();
        await host.PushAsync(modal);

        await host.DispatchToActiveAsync("hello");

        Assert.Empty(root.Events);
        Assert.Single(modal.Events);
        Assert.Equal("hello", modal.Events[0].Event);
    }

    [Fact]
    public async Task PopAsync_DisposesTop_ReactivatesPrevious_AndCanResumeWithEvent()
    {
        await using var host = new DialogHost<string>();
        var root = new RecordingDialogFlow<string>();
        await host.StartAsync(root);

        var modal = new RecordingDialogFlow<string>();
        await host.PushAsync(modal);

        await host.PopAsync(resumeEvent: "resume");

        Assert.Equal(1, host.Depth);
        Assert.True(modal.IsDisposed);
        Assert.Equal(2, root.ActivatedCount);
        Assert.Single(root.Events);
        Assert.Equal("resume", root.Events[0].Event);
    }

    [Fact]
    public async Task ReplaceTopAsync_DisposesPreviousTop_AndActivatesNewTop()
    {
        await using var host = new DialogHost<string>();
        var root = new RecordingDialogFlow<string>();
        await host.StartAsync(root);

        var first = new RecordingDialogFlow<string>();
        await host.PushAsync(first);

        var second = new RecordingDialogFlow<string>();
        await host.ReplaceTopAsync(second);

        Assert.Equal(2, host.Depth);
        Assert.True(first.IsDisposed);
        Assert.Equal(1, first.DeactivatedCount);
        Assert.Equal(1, second.ActivatedCount);
        Assert.Equal(1, root.DeactivatedCount);
        Assert.Equal(0, second.DeactivatedCount);
    }

    [Fact]
    public async Task BroadcastAsync_SendsToAllModules()
    {
        await using var host = new DialogHost<string>();
        var root = new RecordingDialogFlow<string>();
        await host.StartAsync(root);

        var modal = new RecordingDialogFlow<string>();
        await host.PushAsync(modal);

        await host.BroadcastAsync("broadcast", DispatchMode.Interrupt);

        Assert.Single(root.Events);
        Assert.Single(modal.Events);
        Assert.Equal(DispatchMode.Interrupt, root.Events[0].Mode);
        Assert.Equal(DispatchMode.Interrupt, modal.Events[0].Mode);
        Assert.Equal("broadcast", root.Events[0].Event);
    }

    [Fact]
    public async Task BroadcastAsync_WhenEarlierFlowPopsLaterFlow_SkipsRemovedFlow()
    {
        await using var host = new DialogHost<string>();
        var root = new ControllableDialogFlow<string>
        {
            DispatchCallback = (_, _, cancellationToken) => host.PopAsync(cancellationToken)
        };
        await host.StartAsync(root);

        var modal = new RecordingDialogFlow<string>();
        await host.PushAsync(modal);

        await host.BroadcastAsync("broadcast");

        Assert.Equal(1, host.Depth);
        Assert.Equal(2, root.ActivatedCount);
        Assert.Equal(1, root.DeactivatedCount);
        Assert.True(modal.IsDisposed);
        Assert.Empty(modal.Events);
    }

    [Fact]
    public async Task BroadcastAsync_WhenNotStarted_IsNoOp()
    {
        await using var host = new DialogHost<string>();

        await host.BroadcastAsync("noop");

        Assert.Equal(0, host.Depth);
    }

    [Fact]
    public async Task BroadcastAsync_WhenDisposed_Throws()
    {
        await using var host = new DialogHost<string>();
        await host.StartAsync(new RecordingDialogFlow<string>());

        await host.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await host.BroadcastAsync("evt"));
    }

    [Fact]
    public async Task DispatchToActiveAsync_WhenDisposed_Throws()
    {
        await using var host = new DialogHost<string>();
        await host.StartAsync(new RecordingDialogFlow<string>());

        await host.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await host.DispatchToActiveAsync("evt"));
    }

    [Fact]
    public async Task StartAsync_WhenActivationFails_DisposesFlowAndResetsDepth()
    {
        await using var host = new DialogHost<string>();
        var root = new ControllableDialogFlow<string> { ActivateException = new InvalidOperationException("boom") };

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await host.StartAsync(root));

        Assert.Equal(0, host.Depth);
        Assert.Equal(1, root.DisposedCount);
    }

    [Fact]
    public async Task PushAsync_WhenHostIsNotStarted_StartsHostWithFlow()
    {
        await using var host = new DialogHost<string>();
        var root = new RecordingDialogFlow<string>();

        await host.PushAsync(root);

        Assert.Equal(1, host.Depth);
        Assert.Equal(1, root.ActivatedCount);
    }

    [Fact]
    public async Task PushAsync_WhenNotStartedAndActivationFails_DisposesFlowAndResetsDepth()
    {
        await using var host = new DialogHost<string>();
        var failing = new ControllableDialogFlow<string> { ActivateException = new InvalidOperationException("boom") };

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await host.PushAsync(failing));

        Assert.Equal(0, host.Depth);
        Assert.Equal(1, failing.DisposedCount);
    }

    [Fact]
    public async Task PushAsync_WhenActivationFails_RestoresPreviousFlowAndDisposesNewFlow()
    {
        await using var host = new DialogHost<string>();
        var root = new ControllableDialogFlow<string>();
        await host.StartAsync(root);

        var failing = new ControllableDialogFlow<string> { ActivateException = new InvalidOperationException("boom") };

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await host.PushAsync(failing));

        Assert.Equal(1, host.Depth);
        Assert.Equal(2, root.ActivatedCount);
        Assert.Equal(1, root.DeactivatedCount);
        Assert.Equal(1, failing.DisposedCount);
    }

    [Fact]
    public async Task ReplaceTopAsync_WhenActivationFails_RestoresPreviousFlowAndDisposesNewFlow()
    {
        await using var host = new DialogHost<string>();
        var root = new ControllableDialogFlow<string>();
        await host.StartAsync(root);

        var failing = new ControllableDialogFlow<string> { ActivateException = new InvalidOperationException("boom") };

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await host.ReplaceTopAsync(failing));

        Assert.Equal(1, host.Depth);
        Assert.Equal(2, root.ActivatedCount);
        Assert.Equal(1, root.DeactivatedCount);
        Assert.Equal(1, failing.DisposedCount);
    }

    [Fact]
    public async Task ReplaceTopAsync_WhenPreviousFlowDisposeFails_KeepsReplacementActive()
    {
        await using var host = new DialogHost<string>();
        var root = new ControllableDialogFlow<string> { DisposeException = new InvalidOperationException("dispose") };
        await host.StartAsync(root);

        var replacement = new RecordingDialogFlow<string>();

        await host.ReplaceTopAsync(replacement);
        await host.DispatchToActiveAsync("active");

        Assert.Equal(1, host.Depth);
        Assert.Equal(1, root.DeactivatedCount);
        Assert.Equal(1, root.DisposedCount);
        Assert.Equal(1, replacement.ActivatedCount);
        Assert.Single(replacement.Events);
        Assert.Equal("active", replacement.Events[0].Event);
    }

    [Fact]
    public async Task DisposeAsync_BestEffortTeardownWhenCallbacksThrow()
    {
        await using var host = new DialogHost<string>();
        var root = new ControllableDialogFlow<string>();
        await host.StartAsync(root);

        var modal = new ControllableDialogFlow<string>
        {
            DeactivateException = new InvalidOperationException("deactivate"),
            DisposeException = new InvalidOperationException("dispose")
        };
        await host.PushAsync(modal);

        await host.DisposeAsync();

        Assert.Equal(0, host.Depth);
    }

    [Fact]
    public async Task DisposeAsync_WhenHostHasInactiveFlows_DeactivatesOnlyActiveFlow()
    {
        await using var host = new DialogHost<string>();
        var root = new ControllableDialogFlow<string>();
        await host.StartAsync(root);

        var modal = new ControllableDialogFlow<string>();
        await host.PushAsync(modal);

        await host.DisposeAsync();

        Assert.Equal(0, host.Depth);
        Assert.Equal(1, root.ActivatedCount);
        Assert.Equal(1, root.DeactivatedCount);
        Assert.Equal(1, root.DisposedCount);
        Assert.Equal(1, modal.ActivatedCount);
        Assert.Equal(1, modal.DeactivatedCount);
        Assert.Equal(1, modal.DisposedCount);
    }

    [Fact]
    public async Task StartAsync_WhenAlreadyStarted_Throws()
    {
        await using var host = new DialogHost<string>();
        await host.StartAsync(new RecordingDialogFlow<string>());

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await host.StartAsync(new RecordingDialogFlow<string>()));
    }

    [Fact]
    public async Task DispatchToActiveAsync_WhenNotStarted_Throws()
    {
        await using var host = new DialogHost<string>();

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await host.DispatchToActiveAsync("evt"));
    }

    [Fact]
    public async Task PushAsync_WhenDisposed_Throws()
    {
        await using var host = new DialogHost<string>();
        await host.StartAsync(new RecordingDialogFlow<string>());

        await host.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await host.PushAsync(new RecordingDialogFlow<string>()));
    }

    [Fact]
    public async Task PushAsync_WhenCalledFromActivationCallback_ThrowsReentrancyError()
    {
        await using var host = new DialogHost<string>();
        await host.StartAsync(new RecordingDialogFlow<string>());

        var reentrant = new ReentrantDialogFlow<string>(async () =>
        {
            await host.PushAsync(new RecordingDialogFlow<string>());
        });

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await host.PushAsync(reentrant));

        Assert.Equal(1, host.Depth);
    }

    [Fact]
    public async Task DisposeAsync_WhenCalledFromActivationCallback_ThrowsReentrancyError()
    {
        await using var host = new DialogHost<string>();
        await host.StartAsync(new RecordingDialogFlow<string>());

        var reentrant = new ReentrantDialogFlow<string>(async () =>
        {
            await host.DisposeAsync();
        });

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await host.PushAsync(reentrant));

        Assert.Equal(1, host.Depth);
    }

    [Fact]
    public async Task PopAsync_WhenNotStarted_IsNoOp()
    {
        await using var host = new DialogHost<string>();

        await host.PopAsync();

        Assert.Equal(0, host.Depth);
    }

    [Fact]
    public async Task PopAsync_WhenOnlyRoot_RemovesRootAndDoesNotDispatchResumeEvent()
    {
        await using var host = new DialogHost<string>();
        var root = new RecordingDialogFlow<string>();
        await host.StartAsync(root);

        await host.PopAsync(resumeEvent: "resume");

        Assert.Equal(0, host.Depth);
        Assert.True(root.IsDisposed);
        Assert.Empty(root.Events);
    }

    [Fact]
    public async Task PopAsync_WhenRemovedFlowDisposeFails_ReactivatesPreviousFlowAndDoesNotThrow()
    {
        await using var host = new DialogHost<string>();
        var root = new RecordingDialogFlow<string>();
        await host.StartAsync(root);

        var modal = new ControllableDialogFlow<string> { DisposeException = new InvalidOperationException("dispose") };
        await host.PushAsync(modal);

        await host.PopAsync(resumeEvent: "resume");

        Assert.Equal(1, host.Depth);
        Assert.Equal(2, root.ActivatedCount);
        Assert.Single(root.Events);
        Assert.Equal("resume", root.Events[0].Event);
        Assert.Equal(1, modal.DeactivatedCount);
        Assert.Equal(1, modal.DisposedCount);
    }

    [Fact]
    public async Task ReplaceTopAsync_WhenNotStarted_StartsHostWithModule()
    {
        await using var host = new DialogHost<string>();
        var root = new RecordingDialogFlow<string>();

        await host.ReplaceTopAsync(root);

        Assert.Equal(1, host.Depth);
        Assert.Equal(1, root.ActivatedCount);
    }

    [Fact]
    public async Task ReplaceTopAsync_WhenNotStartedAndActivationFails_DisposesFlowAndResetsDepth()
    {
        await using var host = new DialogHost<string>();
        var failing = new ControllableDialogFlow<string> { ActivateException = new InvalidOperationException("boom") };

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await host.ReplaceTopAsync(failing));

        Assert.Equal(0, host.Depth);
        Assert.Equal(1, failing.DisposedCount);
    }

    [Fact]
    public async Task PopAsync_WhenNoResumeEvent_DoesNotDispatchResumeEvent()
    {
        await using var host = new DialogHost<string>();
        var root = new RecordingDialogFlow<string>();
        await host.StartAsync(root);

        var modal = new RecordingDialogFlow<string>();
        await host.PushAsync(modal);

        await host.PopAsync();

        Assert.Equal(1, host.Depth);
        Assert.Empty(root.Events);
    }

    [Fact]
    public async Task PopAsync_WhenResumeModeIsInterrupt_ResumesWithInterruptMode()
    {
        await using var host = new DialogHost<string>();
        var root = new RecordingDialogFlow<string>();
        await host.StartAsync(root);

        var modal = new RecordingDialogFlow<string>();
        await host.PushAsync(modal);

        await host.PopAsync(resumeEvent: "resume", resumeMode: DispatchMode.Interrupt);

        Assert.Equal(1, host.Depth);
        Assert.Single(root.Events);
        Assert.Equal("resume", root.Events[0].Event);
        Assert.Equal(DispatchMode.Interrupt, root.Events[0].Mode);
    }

    [Fact]
    public async Task PopAsync_WhenReactivatingPreviousFlowFails_RestoresPoppedFlow()
    {
        await using var host = new DialogHost<string>();
        var root = new ControllableDialogFlow<string>();
        await host.StartAsync(root);

        var modal = new ControllableDialogFlow<string>();
        await host.PushAsync(modal);

        root.ActivateException = new InvalidOperationException("reactivate");

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await host.PopAsync().AsTask());

        Assert.Equal(2, host.Depth);
        Assert.Equal(2, root.ActivatedCount);
        Assert.Equal(1, root.DeactivatedCount);
        Assert.Equal(2, modal.ActivatedCount);
        Assert.Equal(1, modal.DeactivatedCount);
        Assert.Equal(0, modal.DisposedCount);

        await host.DispatchToActiveAsync("still-active");

        Assert.Single(modal.Events);
        Assert.Empty(root.Events);
        Assert.Equal("still-active", modal.Events[0].Event);
    }
}
