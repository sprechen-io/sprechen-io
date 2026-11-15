using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class MicGateLifecycleObserverTests
{
    [Fact]
    public async Task Dispose_CancelsTheRealRecorderCloseAndRestoresTheOpenGate()
    {
        var tts = new RecordingPlaybackState();
        var earcon = new RecordingEarconPlaybackState();
        var jsRuntime = new CancellationAwareMicGateJsRuntime();
        var recorder = new MicrophoneRecorder(jsRuntime);
        await recorder.RequestAccessAsync(CancellationToken.None);
        var observer = new MicGateLifecycleObserver(
            tts,
            earcon,
            new StubMicGatePolicyResolver(MicGatePolicy.HardOff),
            recorder,
            NullLogger<MicGateLifecycleObserver>.Instance);

        tts.NotifyPlaybackStarted();
        await jsRuntime.WaitForCloseAsync();
        observer.Dispose();
        await jsRuntime.WaitForOpenAsync();
        tts.NotifyPlaybackCompleted();

        Assert.Equal([false, true], jsRuntime.GateWrites);
    }

    [Fact]
    public async Task Dispose_AfterCloseWithoutASession_RestoresOpenBeforeLaterAcquisition()
    {
        var tts = new RecordingPlaybackState();
        var jsRuntime = new CancellationAwareMicGateJsRuntime();
        var recorder = new MicrophoneRecorder(jsRuntime);
        var observer = new MicGateLifecycleObserver(
            tts,
            new RecordingEarconPlaybackState(),
            new StubMicGatePolicyResolver(MicGatePolicy.HardOff),
            recorder,
            NullLogger<MicGateLifecycleObserver>.Instance);

        tts.NotifyPlaybackStarted();
        observer.Dispose();
        await recorder.RequestAccessAsync(CancellationToken.None);
        await jsRuntime.WaitForOpenAsync();

        Assert.Equal([true], jsRuntime.GateWrites);
    }

    [Fact]
    public void RisingEdge_WhenPolicyIsHardOff_GatesMicOnce()
    {
        var tts = new RecordingPlaybackState();
        var earcon = new RecordingEarconPlaybackState();
        var bridge = new RecordingMicGateBrowserBridge();
        using var observer = NewObserver(tts, earcon, MicGatePolicy.HardOff, bridge);

        tts.NotifyPlaybackStarted();
        bridge.WaitForCalls(1, TimeSpan.FromSeconds(2));

        Assert.Equal([false], bridge.Snapshot());
        Assert.True(observer.IsHardOff);
    }

    [Fact]
    public void RisingEdge_WhenPolicyIsHeuristic_DoesNotInvokeBridge()
    {
        var tts = new RecordingPlaybackState();
        var earcon = new RecordingEarconPlaybackState();
        var bridge = new RecordingMicGateBrowserBridge();
        using var observer = NewObserver(tts, earcon, MicGatePolicy.Heuristic, bridge);

        tts.NotifyPlaybackStarted();
        // Unerwarteten asynchronen Aufrufen vor der Prüfung kurz Zeit geben.
        Thread.Sleep(50);

        Assert.Empty(bridge.Snapshot());
        Assert.False(observer.IsHardOff);
    }

    [Fact]
    public void FallingEdge_AfterHardOff_ReleasesMicOnce()
    {
        var tts = new RecordingPlaybackState();
        var earcon = new RecordingEarconPlaybackState();
        var bridge = new RecordingMicGateBrowserBridge();
        using var observer = NewObserver(tts, earcon, MicGatePolicy.HardOff, bridge);

        tts.NotifyPlaybackStarted();
        bridge.WaitForCalls(1, TimeSpan.FromSeconds(2));
        tts.NotifyPlaybackCompleted();
        bridge.WaitForCalls(2, TimeSpan.FromSeconds(2));

        Assert.Equal([false, true], bridge.Snapshot());
        Assert.False(observer.IsHardOff);
    }

    [Fact]
    public void OverlappingTtsAndEarcon_DoesNotEmitPrematureRelease()
    {
        // Solange TTS weiterläuft, darf das zwischenzeitliche Ende des Earcons die Sperre nicht lösen.
        var tts = new RecordingPlaybackState();
        var earcon = new RecordingEarconPlaybackState();
        var bridge = new RecordingMicGateBrowserBridge();
        using var observer = NewObserver(tts, earcon, MicGatePolicy.HardOff, bridge);

        tts.NotifyPlaybackStarted();
        bridge.WaitForCalls(1, TimeSpan.FromSeconds(2));

        Assert.Equal([false], bridge.Snapshot());
        Assert.True(observer.IsHardOff);

        earcon.NotifyPlaybackStarted();
        Thread.Sleep(20);

        Assert.Equal([false], bridge.Snapshot());
        Assert.True(observer.IsHardOff);

        earcon.NotifyPlaybackCompleted();
        Thread.Sleep(20);

        Assert.Equal([false], bridge.Snapshot());
        Assert.True(observer.IsHardOff);

        tts.NotifyPlaybackCompleted();
        bridge.WaitForCalls(2, TimeSpan.FromSeconds(2));

        Assert.Equal([false, true], bridge.Snapshot());
        Assert.False(observer.IsHardOff);
    }

    [Fact]
    public void IsHardOff_TracksRisingEdgeSynchronouslyAndFallingEdgeAfterBrowserConfirmation()
    {
        var tts = new RecordingPlaybackState();
        var earcon = new RecordingEarconPlaybackState();
        var bridge = new RecordingMicGateBrowserBridge();
        using var observer = NewObserver(tts, earcon, MicGatePolicy.HardOff, bridge);

        Assert.False(observer.IsHardOff);

        tts.NotifyPlaybackStarted();

        Assert.True(observer.IsHardOff, "IsHardOff must reflect the rising edge synchronously inside the observer's lock.");

        tts.NotifyPlaybackCompleted();
        bridge.WaitForCalls(2, TimeSpan.FromSeconds(2));
        Assert.True(SpinWait.SpinUntil(() => !observer.IsHardOff, TimeSpan.FromSeconds(2)));

        Assert.False(observer.IsHardOff);
    }

    [Fact]
    public async Task RapidCloseThenOpen_PublishesTheLatestDesiredStateWithoutWaitingForThePriorCall()
    {
        var tts = new RecordingPlaybackState();
        var bridge = new ControlledMicGateBrowserBridge();
        using var observer = NewObserver(tts, new RecordingEarconPlaybackState(), MicGatePolicy.HardOff, bridge);

        tts.NotifyPlaybackStarted();
        await bridge.WaitForCallsAsync(1);
        var wait = observer.WaitUntilOpenAsync(CancellationToken.None).AsTask();

        tts.NotifyPlaybackCompleted();
        await bridge.WaitForCallsAsync(2);
        Assert.Equal([false, true], bridge.Snapshot());
        Assert.False(wait.IsCompleted);

        bridge.CompleteCall(0);
        Assert.False(wait.IsCompleted);

        bridge.CompleteCall(1);
        await wait.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.False(observer.IsHardOff);
    }

    [Fact]
    public async Task FailedClose_FaultsCapturesWaitingBeforeArmingUntilPlaybackEnds()
    {
        var tts = new RecordingPlaybackState();
        var bridge = new ControlledMicGateBrowserBridge();
        using var observer = NewObserver(tts, new RecordingEarconPlaybackState(), MicGatePolicy.HardOff, bridge);

        tts.NotifyPlaybackStarted();
        await bridge.WaitForCallsAsync(1);
        var existingWaiter = observer.WaitUntilOpenAsync(CancellationToken.None).AsTask();
        var gateFailure = new InvalidOperationException("gate close failed and released the session");

        bridge.FailCall(0, gateFailure);

        var existingFailure = await Assert.ThrowsAsync<InvalidOperationException>(() => existingWaiter);
        var laterFailure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => observer.WaitUntilOpenAsync(CancellationToken.None).AsTask());
        Assert.Same(gateFailure, existingFailure);
        Assert.Same(gateFailure, laterFailure);

        tts.NotifyPlaybackCompleted();
        await bridge.WaitForCallsAsync(2);
        bridge.CompleteCall(1);
        Assert.True(SpinWait.SpinUntil(() => !observer.IsHardOff, TimeSpan.FromSeconds(2)));
        await observer.WaitUntilOpenAsync(CancellationToken.None);

        Assert.False(observer.IsHardOff);
    }

    [Fact]
    public async Task FailedBridgeCall_IsLoggedAtWarning_NotOnlyDeliveredToWaiters()
    {
        var tts = new RecordingPlaybackState();
        var bridge = new ControlledMicGateBrowserBridge();
        var logger = new CapturingMicGateLogger();
        using var observer = new MicGateLifecycleObserver(
            tts,
            new RecordingEarconPlaybackState(),
            new StubMicGatePolicyResolver(MicGatePolicy.HardOff),
            bridge,
            logger);

        tts.NotifyPlaybackStarted();
        await bridge.WaitForCallsAsync(1);
        var waiter = observer.WaitUntilOpenAsync(CancellationToken.None).AsTask();
        var gateFailure = new InvalidOperationException("gate close failed");
        bridge.FailCall(0, gateFailure);
        Assert.Same(gateFailure, await Assert.ThrowsAsync<InvalidOperationException>(() => waiter));

        var warning = Assert.Single(logger.Entries, entry => entry.Level == LogLevel.Warning);
        Assert.Same(gateFailure, warning.Exception);
        Assert.Contains("MicGate browser bridge call failed", warning.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StartEndStartEnd_ReleasesWaitersOnlyForTheLatestDesiredStateConfirmation()
    {
        var tts = new RecordingPlaybackState();
        var bridge = new ControlledMicGateBrowserBridge();
        using var observer = NewObserver(tts, new RecordingEarconPlaybackState(), MicGatePolicy.HardOff, bridge);

        tts.NotifyPlaybackStarted();
        await bridge.WaitForCallsAsync(1);
        var wait = observer.WaitUntilOpenAsync(CancellationToken.None).AsTask();
        tts.NotifyPlaybackCompleted();
        await bridge.WaitForCallsAsync(2);

        tts.NotifyPlaybackStarted();
        await bridge.WaitForCallsAsync(3);
        Assert.Equal([false, true, false], bridge.Snapshot());
        Assert.False(wait.IsCompleted);

        tts.NotifyPlaybackCompleted();
        await bridge.WaitForCallsAsync(4);
        Assert.Equal([false, true, false, true], bridge.Snapshot());
        Assert.False(wait.IsCompleted);

        bridge.CompleteCall(1);
        bridge.CompleteCall(2);
        bridge.CompleteCall(0);
        Assert.False(wait.IsCompleted);

        bridge.CompleteCall(3);
        await wait.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.False(observer.IsHardOff);
    }

    [Fact]
    public async Task Dispose_FaultsExistingWaitersAndRejectsNewWaits()
    {
        var tts = new RecordingPlaybackState();
        var bridge = new ControlledMicGateBrowserBridge();
        var observer = NewObserver(
            tts,
            new RecordingEarconPlaybackState(),
            MicGatePolicy.HardOff,
            bridge);

        tts.NotifyPlaybackStarted();
        await bridge.WaitForCallsAsync(1);
        var pending = observer.WaitUntilOpenAsync(CancellationToken.None).AsTask();

        observer.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => pending.WaitAsync(TimeSpan.FromSeconds(5)));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => bridge.WaitForCallCompletionAsync(0).WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Throws<ObjectDisposedException>(
            () => observer.WaitUntilOpenAsync(CancellationToken.None));
    }

    private static MicGateLifecycleObserver NewObserver(
        RecordingPlaybackState tts,
        RecordingEarconPlaybackState earcon,
        MicGatePolicy policy,
        IMicGateBrowserBridge bridge)
    {
        return new MicGateLifecycleObserver(
            tts,
            earcon,
            new StubMicGatePolicyResolver(policy),
            bridge,
            NullLogger<MicGateLifecycleObserver>.Instance);
    }
}
