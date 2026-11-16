using Bunit;
using MicroSharp.Browser;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Sprechen.Speech.App.BrowserInfrastructure.Extensions;
using Sprechen.Speech.Runtime.Tests.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class MicrophoneRecorderTests : TestContext
{
    [Fact]
    public async Task StartAsync_UsesDefaultTimesliceMilliseconds()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", invocation =>
            invocation.Arguments.Count == 1
            && invocation.Arguments[0] is true).SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.Setup<string>("micRecorder.connect", invocation =>
            invocation.Arguments.Count == 3 &&
            invocation.Arguments[2] is int timeslice &&
            timeslice == 20).SetResult("audio/pcm;rate=16000");

        JSInterop.SetupVoid("micRecorder.disconnect", _ => true).SetVoidResult();

        var recorder = MicrophoneTestRecorder.Create(JSInterop.JSRuntime);

        await using var recording = await recorder.StartAsync(CancellationToken.None);

        Assert.Equal("audio/pcm;rate=16000", recording.MimeType);
    }

    [Fact]
    public async Task ReleaseAsync_InvokesMicRecorderRelease()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true).SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.SetupVoid("micRecorder.release", _ => true).SetVoidResult();

        var recorder = MicrophoneTestRecorder.Create(JSInterop.JSRuntime);

        await recorder.RequestAccessAsync(CancellationToken.None);
        await recorder.ReleaseAsync(CancellationToken.None);

        JSInterop.VerifyInvoke("micRecorder.release", calledTimes: 1);
    }

    [Fact]
    public async Task ReleaseAsync_WhenBrowserContextIsGone_TreatsTheSessionAsReleased()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true).SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.SetupVoid("micRecorder.release", _ => true)
            .SetException(new JSDisconnectedException("browser context is gone"));

        var recorder = MicrophoneTestRecorder.Create(JSInterop.JSRuntime);

        await recorder.RequestAccessAsync(CancellationToken.None);
        await recorder.ReleaseAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ReleaseAsync_WhenTeardownFails_RetainsTheSessionForRetry()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true).SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        var release = JSInterop.SetupVoid("micRecorder.release", _ => true);
        release.SetException(new JSException("teardown failed"));
        JSInterop.Setup<bool>("micRecorder.isLive", _ => true).SetResult(true);
        var logger = new RecordingLogger<MicrophoneRecorder>();
        var recorder = MicrophoneTestRecorder.Create(JSInterop.JSRuntime, logger);

        await recorder.RequestAccessAsync(CancellationToken.None);
        await Assert.ThrowsAsync<JSException>(() => recorder.ReleaseAsync(CancellationToken.None));
        var reacquisitionFailure = await Assert.ThrowsAsync<MicrophoneAcquisitionException>(
            () => recorder.RequestAccessAsync(CancellationToken.None));
        Assert.Equal(MicrophoneAcquisitionFailureCode.Unavailable, reacquisitionFailure.Code);
        Assert.IsType<JSException>(reacquisitionFailure.InnerException);
        Assert.DoesNotContain(
            logger.Entries,
            entry => entry.Message.Contains("fully released", StringComparison.Ordinal));

        release.SetVoidResult();
        await recorder.ReleaseAsync(CancellationToken.None);
        await recorder.RequestAccessAsync(CancellationToken.None);

        Assert.Equal(
            3,
            JSInterop.Invocations.Count(invocation => invocation.Identifier == "micRecorder.release"));
        Assert.Contains(
            logger.Entries,
            entry => entry.Message.Contains("fully released", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ReleaseAsync_WhenTeardownFailsAndSessionLooksLive_RetriesBeforeNextStart()
    {
        var acquisition = JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true);
        acquisition.SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.Setup<string>("micRecorder.connect", _ => true).SetResult("audio/pcm;rate=16000");
        JSInterop.SetupVoid("micRecorder.disconnect", _ => true).SetVoidResult();
        var release = JSInterop.SetupVoid("micRecorder.release", _ => true);
        release.SetException(new JSException("teardown failed"));
        JSInterop.Setup<bool>("micRecorder.isLive", _ => true).SetResult(true);
        var recorder = MicrophoneTestRecorder.Create(JSInterop.JSRuntime);

        await recorder.RequestAccessAsync(CancellationToken.None);
        await Assert.ThrowsAsync<JSException>(() => recorder.ReleaseAsync(CancellationToken.None));
        release.SetVoidResult();
        acquisition.SetResult(new MicrophoneAcquisitionDto { SessionId = "2" });

        await using var restarted = await recorder.StartAsync(CancellationToken.None);

        JSInterop.VerifyInvoke("micRecorder.release", calledTimes: 2);
        JSInterop.VerifyInvoke("micRecorder.acquire", calledTimes: 2);
        JSInterop.VerifyNotInvoke("micRecorder.isLive");
    }

    [Fact]
    public async Task ReleaseAsync_WhenTeardownFails_EndsTheDisconnectedRecording()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true)
            .SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.Setup<string>("micRecorder.connect", _ => true)
            .SetResult("audio/pcm;rate=16000");
        var release = JSInterop.SetupVoid("micRecorder.release", _ => true);
        release.SetException(new JSException("teardown failed"));
        var recorder = MicrophoneTestRecorder.Create(JSInterop.JSRuntime);
        var recording = await recorder.StartRecordingAsync(20, CancellationToken.None);

        await Assert.ThrowsAsync<JSException>(() => recorder.ReleaseAsync(CancellationToken.None));

        Assert.True(recording.Completion.IsCompleted);
        Assert.True(recording.IsDisposed);

        release.SetVoidResult();
        await recorder.ReleaseAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ReleaseAsync_WhenTeardownFails_AllowsStartAfterDeadSessionReacquisition()
    {
        var acquisition = JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true);
        acquisition.SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.Setup<string>("micRecorder.connect", _ => true)
            .SetResult("audio/pcm;rate=16000");
        JSInterop.SetupVoid("micRecorder.disconnect", _ => true).SetVoidResult();
        var isLive = JSInterop.Setup<bool>("micRecorder.isLive", _ => true);
        isLive.SetResult(false);
        var release = JSInterop.SetupVoid("micRecorder.release", _ => true);
        release.SetException(new JSException("teardown failed"));
        var recorder = MicrophoneTestRecorder.Create(JSInterop.JSRuntime);
        var first = await recorder.StartRecordingAsync(20, CancellationToken.None);

        await Assert.ThrowsAsync<JSException>(() => recorder.ReleaseAsync(CancellationToken.None));
        Assert.True(first.Completion.IsCompleted);
        release.SetVoidResult();
        acquisition.SetResult(new MicrophoneAcquisitionDto { SessionId = "2" });

        await recorder.RequestAccessAsync(CancellationToken.None);
        isLive.SetResult(true);
        await using var restarted = await recorder.StartAsync(CancellationToken.None);

        Assert.Equal(first.MimeType, restarted.MimeType);
        JSInterop.VerifyInvoke("micRecorder.release", calledTimes: 2);
        JSInterop.VerifyInvoke("micRecorder.acquire", calledTimes: 2);
    }

    [Fact]
    public async Task ReleaseAsync_WithCancelledCallerToken_StillReleasesTheBrowserSession()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true).SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.SetupVoid("micRecorder.release", _ => true).SetVoidResult();
        var recorder = MicrophoneTestRecorder.Create(JSInterop.JSRuntime);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await recorder.RequestAccessAsync(CancellationToken.None);
        await recorder.ReleaseAsync(cancellation.Token);

        JSInterop.VerifyInvoke("micRecorder.release", calledTimes: 1);
    }

    [Fact]
    public async Task ReadAsync_ReturnsTheAppliedTrackProcessing()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true).SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.Setup<MicrophoneTrackProcessingDto>("micRecorder.trackProcessing", _ => true)
            .SetResult(new MicrophoneTrackProcessingDto
            {
                EchoCancellation = true,
                NoiseSuppression = true,
                AutoGainControl = false
            });

        var recorder = MicrophoneTestRecorder.Create(JSInterop.JSRuntime);

        await recorder.RequestAccessAsync(CancellationToken.None);
        var processing = await recorder.ReadAsync(CancellationToken.None);

        Assert.True(processing.EchoCancellation);
        Assert.True(processing.NoiseSuppression);
        Assert.False(processing.AutoGainControl);
    }

    [Fact]
    public async Task ReadAsync_ReturnsUnknownWhenTheBridgeIsGone()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true).SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.Setup<MicrophoneTrackProcessingDto>("micRecorder.trackProcessing", _ => true)
            .SetException(new JSException("disconnected"));

        var recorder = MicrophoneTestRecorder.Create(JSInterop.JSRuntime);

        await recorder.RequestAccessAsync(CancellationToken.None);
        var processing = await recorder.ReadAsync(CancellationToken.None);

        Assert.Equal(CaptureTrackProcessing.Unknown, processing);
    }

    [Fact]
    public async Task StopAsync_DuringAcquisition_StopsThePendingStartAndKeepsTheSessionReusable()
    {
        var jsRuntime = new ControlledMicrophoneLifecycleJsRuntime();
        var recorder = MicrophoneTestRecorder.Create(jsRuntime);

        var start = recorder.StartAsync(CancellationToken.None);
        await jsRuntime.WaitForAcquisitionAsync();
        var stop = recorder.StopAsync(CancellationToken.None);

        jsRuntime.CompleteAcquisition();

        await Assert.ThrowsAsync<InvalidOperationException>(() => start);
        await stop;
        await using var laterRecording = await recorder.StartAsync(CancellationToken.None);

        Assert.Equal(1, jsRuntime.AcquireCallCount);
        Assert.Equal(1, jsRuntime.ConnectCallCount);
        Assert.True(jsRuntime.DisconnectCallCount >= 1);
    }

    [Fact]
    public async Task ReleaseAsync_DuringWarmup_ReleasesTheAcquiredSession()
    {
        var jsRuntime = new ControlledMicrophoneLifecycleJsRuntime();
        var recorder = MicrophoneTestRecorder.Create(jsRuntime);

        var warmup = recorder.RequestAccessAsync(CancellationToken.None);
        await jsRuntime.WaitForAcquisitionAsync();
        var release = recorder.ReleaseAsync(CancellationToken.None);

        jsRuntime.CompleteAcquisition();

        await Task.WhenAll(warmup, release);
        Assert.Equal(1, jsRuntime.AcquireCallCount);
        Assert.Equal(1, jsRuntime.ReleaseCallCount);
        Assert.Equal(0, jsRuntime.ConnectCallCount);
    }

    [Fact]
    public async Task ReleaseAsync_DuringPendingWarmup_HonorsTheCallerAndStillReleasesEventually()
    {
        var jsRuntime = new ControlledMicrophoneLifecycleJsRuntime();
        var recorder = MicrophoneTestRecorder.Create(jsRuntime);
        var warmup = recorder.RequestAccessAsync(CancellationToken.None);
        await jsRuntime.WaitForAcquisitionAsync();
        using var cancellation = new CancellationTokenSource();

        var release = recorder.ReleaseAsync(cancellation.Token);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => release.WaitAsync(TimeSpan.FromSeconds(5)));
        jsRuntime.CompleteAcquisition();
        await recorder.ReleaseAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
        await warmup;

        Assert.Equal(1, jsRuntime.ReleaseCallCount);
    }

    [Fact]
    public async Task RequestAccessAsync_WhenBrowserReleaseNeverCompletes_PublishesUnavailableAndRemainsRetryable()
    {
        var jsRuntime = new ControlledMicrophoneLifecycleJsRuntime();
        jsRuntime.CompleteAcquisition();
        jsRuntime.HoldBrowserRelease();
        var recorder = MicrophoneTestRecorder.Create(
            jsRuntime,
            releaseProgressTimeout: TimeSpan.FromMilliseconds(20));
        await recorder.RequestAccessAsync(CancellationToken.None);

        await Assert.ThrowsAsync<TimeoutException>(
            () => recorder.ReleaseAsync(CancellationToken.None));
        var unavailable = await Assert.ThrowsAsync<MicrophoneAcquisitionException>(
            () => recorder.RequestAccessAsync(CancellationToken.None));

        Assert.Equal(MicrophoneAcquisitionFailureCode.Unavailable, unavailable.Code);
        Assert.IsType<TimeoutException>(unavailable.InnerException);
        Assert.Equal(2, jsRuntime.ReleaseCallCount);

        jsRuntime.CompleteBrowserRelease();
        await recorder.RequestAccessAsync(CancellationToken.None);

        Assert.Equal(3, jsRuntime.ReleaseCallCount);
        Assert.Equal(2, jsRuntime.AcquireCallCount);
    }

    [Fact]
    public async Task RequestAccessAsync_WhenPendingAcquisitionNeverCompletes_RemainsTypedAndRetryable()
    {
        var jsRuntime = new ControlledMicrophoneLifecycleJsRuntime();
        var recorder = MicrophoneTestRecorder.Create(
            jsRuntime,
            releaseProgressTimeout: TimeSpan.FromMilliseconds(20));
        using var accessCancellation = new CancellationTokenSource();
        var access = recorder.RequestAccessAsync(accessCancellation.Token);
        await jsRuntime.WaitForAcquisitionAsync();
        accessCancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => access);

        await Assert.ThrowsAsync<TimeoutException>(
            () => recorder.ReleaseAsync(CancellationToken.None));
        var unavailable = await Assert.ThrowsAsync<MicrophoneAcquisitionException>(
            () => recorder.RequestAccessAsync(CancellationToken.None));

        Assert.Equal(MicrophoneAcquisitionFailureCode.Unavailable, unavailable.Code);
        Assert.IsType<TimeoutException>(unavailable.InnerException);
        Assert.Equal(0, jsRuntime.ReleaseCallCount);

        jsRuntime.CompleteAcquisition();
        await recorder.RequestAccessAsync(CancellationToken.None);

        Assert.Equal(1, jsRuntime.ReleaseCallCount);
        Assert.Equal(2, jsRuntime.AcquireCallCount);
    }

    [Fact]
    public async Task ReleaseAsync_WhenPendingAcquisitionCompletesLate_ReleasesWithoutAnotherLifecycleCall()
    {
        var jsRuntime = new ControlledMicrophoneLifecycleJsRuntime();
        var recorder = MicrophoneTestRecorder.Create(
            jsRuntime,
            releaseProgressTimeout: TimeSpan.FromMilliseconds(20));
        using var accessCancellation = new CancellationTokenSource();
        var access = recorder.RequestAccessAsync(accessCancellation.Token);
        await jsRuntime.WaitForAcquisitionAsync();
        accessCancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => access);
        await Assert.ThrowsAsync<TimeoutException>(
            () => recorder.ReleaseAsync(CancellationToken.None));

        jsRuntime.CompleteAcquisition("41");
        await jsRuntime.WaitForBrowserReleaseAsync().WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(1, jsRuntime.ReleaseCallCount);
        Assert.Equal(1, jsRuntime.AcquireCallCount);
    }

    [Fact]
    public async Task ReleaseAsync_DuringPendingAcquisitionRecovery_SerializesBehindTheRecovery()
    {
        var jsRuntime = new ControlledMicrophoneLifecycleJsRuntime();
        var recorder = MicrophoneTestRecorder.Create(
            jsRuntime,
            releaseProgressTimeout: TimeSpan.FromMilliseconds(100));
        using var accessCancellation = new CancellationTokenSource();
        var initialAccess = recorder.RequestAccessAsync(accessCancellation.Token);
        await jsRuntime.WaitForAcquisitionAsync();
        accessCancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => initialAccess);
        await Assert.ThrowsAsync<TimeoutException>(
            () => recorder.ReleaseAsync(CancellationToken.None));

        var recovery = recorder.RequestAccessAsync(CancellationToken.None);
        var release = recorder.ReleaseAsync(CancellationToken.None);
        jsRuntime.CompleteAcquisition();

        await Task.WhenAll(recovery, release);
        await recorder.RequestAccessAsync(CancellationToken.None);

        Assert.Equal(2, jsRuntime.ReleaseCallCount);
        Assert.Equal(3, jsRuntime.AcquireCallCount);
    }

    [Fact]
    public async Task RequestAccessAsync_WhenDeadSessionReleaseStalls_UsesTheQuarantineRetryPath()
    {
        var jsRuntime = new ControlledMicrophoneLifecycleJsRuntime();
        jsRuntime.CompleteAcquisition();
        var recorder = MicrophoneTestRecorder.Create(
            jsRuntime,
            releaseProgressTimeout: TimeSpan.FromMilliseconds(20));
        await recorder.RequestAccessAsync(CancellationToken.None);
        jsRuntime.SetSessionLive(false);
        jsRuntime.HoldBrowserRelease();

        var first = await Assert.ThrowsAsync<MicrophoneAcquisitionException>(
            () => recorder.RequestAccessAsync(CancellationToken.None));
        var second = await Assert.ThrowsAsync<MicrophoneAcquisitionException>(
            () => recorder.RequestAccessAsync(CancellationToken.None));

        Assert.Equal(MicrophoneAcquisitionFailureCode.Unavailable, first.Code);
        Assert.IsType<TimeoutException>(first.InnerException);
        Assert.Equal(MicrophoneAcquisitionFailureCode.Unavailable, second.Code);
        Assert.IsType<TimeoutException>(second.InnerException);
        Assert.Equal(2, jsRuntime.ReleaseCallCount);
        Assert.Equal(1, jsRuntime.AcquireCallCount);

        jsRuntime.CompleteBrowserRelease();
        await recorder.RequestAccessAsync(CancellationToken.None);

        Assert.Equal(3, jsRuntime.ReleaseCallCount);
        Assert.Equal(2, jsRuntime.AcquireCallCount);
    }

    [Fact]
    public async Task RequestAccessAsync_WhenHardOffArrivesDuringAcquisition_GatesBeforePublishingTheSession()
    {
        var jsRuntime = new ControlledMicrophoneLifecycleJsRuntime();
        var recorder = MicrophoneTestRecorder.Create(jsRuntime);

        var access = recorder.RequestAccessAsync(CancellationToken.None);
        await jsRuntime.WaitForAcquisitionAsync();
        await recorder.SetMicGateAsync(false, CancellationToken.None);
        jsRuntime.CompleteAcquisition("11");

        await access;

        Assert.Equal([("11", false)], jsRuntime.MicGateCalls);
    }

    [Fact]
    public async Task StopAsync_DuringRelease_AwaitsTheTerminalReleaseAndReacquiresForTheNextStart()
    {
        var jsRuntime = new ControlledMicrophoneLifecycleJsRuntime();
        jsRuntime.CompleteAcquisition();
        jsRuntime.HoldBrowserRelease();
        var recorder = MicrophoneTestRecorder.Create(jsRuntime);
        await using var recording = await recorder.StartAsync(CancellationToken.None);

        var release = recorder.ReleaseAsync(CancellationToken.None);
        await jsRuntime.WaitForBrowserReleaseAsync();
        var stop = recorder.StopAsync(CancellationToken.None);
        jsRuntime.CompleteBrowserRelease();

        await Task.WhenAll(release, stop);
        await using var restarted = await recorder.StartAsync(CancellationToken.None);

        Assert.Equal(2, jsRuntime.AcquireCallCount);
        Assert.Equal(2, jsRuntime.ConnectCallCount);
        Assert.Equal(1, jsRuntime.ReleaseCallCount);
        Assert.Equal(recording.MimeType, restarted.MimeType);
    }

    [Fact]
    public async Task StartAsync_DuringRelease_IsRejectedUntilReleaseCompletionIsPublished()
    {
        var jsRuntime = new ControlledMicrophoneLifecycleJsRuntime();
        jsRuntime.CompleteAcquisition();
        jsRuntime.HoldBrowserRelease();
        var recorder = MicrophoneTestRecorder.Create(jsRuntime);
        await using var recording = await recorder.StartAsync(CancellationToken.None);

        var release = recorder.ReleaseAsync(CancellationToken.None);
        await jsRuntime.WaitForBrowserReleaseAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => recorder.StartAsync(CancellationToken.None));
        jsRuntime.CompleteBrowserRelease();
        await release;
        await using var restarted = await recorder.StartAsync(CancellationToken.None);

        Assert.Equal(2, jsRuntime.AcquireCallCount);
        Assert.Equal(2, jsRuntime.ConnectCallCount);
    }

    [Fact]
    public async Task StartAsync_WhenReleaseSupersedesConnect_LeavesCleanupWithTheCanonicalReleaseOwner()
    {
        var jsRuntime = new StalledFirstConnectJsRuntime();
        var recorder = MicrophoneTestRecorder.Create(jsRuntime);

        var start = recorder.StartAsync(CancellationToken.None);
        await jsRuntime.WaitForFirstConnectAsync();
        await Assert.ThrowsAsync<JSException>(() => recorder.ReleaseAsync(CancellationToken.None));

        jsRuntime.CompleteFirstConnect();
        await Assert.ThrowsAsync<InvalidOperationException>(() => start);
        Assert.Equal(1, jsRuntime.ReleaseCallCount);

        await recorder.ReleaseAsync(CancellationToken.None);
        await using var restarted = await recorder.StartAsync(CancellationToken.None);

        Assert.Equal(2, jsRuntime.ReleaseCallCount);
        Assert.Equal(2, jsRuntime.AcquireCallCount);
    }

    [Fact]
    public async Task StartAsync_WhenAnotherStartIsAcquiring_RejectsTheSecondStart()
    {
        var jsRuntime = new ControlledMicrophoneLifecycleJsRuntime();
        var recorder = MicrophoneTestRecorder.Create(jsRuntime);

        var first = recorder.StartAsync(CancellationToken.None);
        await jsRuntime.WaitForAcquisitionAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => recorder.StartAsync(CancellationToken.None));
        jsRuntime.CompleteAcquisition();
        await using var recording = await first;

        Assert.Equal(1, jsRuntime.AcquireCallCount);
        Assert.Equal(1, jsRuntime.ConnectCallCount);
    }

    [Fact]
    public async Task StopAsync_WithCancelledCallerToken_StillDisconnectsAndKeepsTheSessionReusable()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true).SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.Setup<string>("micRecorder.connect", _ => true).SetResult("audio/pcm;rate=16000");
        JSInterop.SetupVoid("micRecorder.disconnect", _ => true).SetVoidResult();
        JSInterop.Setup<bool>("micRecorder.isLive", _ => true).SetResult(true);
        var recorder = MicrophoneTestRecorder.Create(JSInterop.JSRuntime);
        await using var recording = await recorder.StartAsync(CancellationToken.None);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await recorder.StopAsync(cancellation.Token);
        await using var restarted = await recorder.StartAsync(CancellationToken.None);

        JSInterop.VerifyInvoke("micRecorder.acquire", calledTimes: 1);
        JSInterop.VerifyInvoke("micRecorder.connect", calledTimes: 2);
        JSInterop.VerifyInvoke("micRecorder.disconnect", calledTimes: 1);
        Assert.Equal(recording.MimeType, restarted.MimeType);
    }

    [Fact]
    public async Task StartAsync_WhenThePreviousDisconnectFailed_RetriesTheRetainedStop()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true).SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.Setup<string>("micRecorder.connect", _ => true).SetResult("audio/pcm;rate=16000");
        var disconnect = JSInterop.SetupVoid("micRecorder.disconnect", _ => true);
        disconnect.SetException(new JSException("disconnect failed"));
        JSInterop.Setup<bool>("micRecorder.isLive", _ => true).SetResult(true);
        var recorder = MicrophoneTestRecorder.Create(JSInterop.JSRuntime);
        await using var recording = await recorder.StartRecordingAsync(20, CancellationToken.None);

        await Assert.ThrowsAsync<JSException>(() => recording.StopAsync(CancellationToken.None).AsTask());
        disconnect.SetVoidResult();
        await using var restarted = await recorder.StartAsync(CancellationToken.None);

        JSInterop.VerifyInvoke("micRecorder.acquire", calledTimes: 1);
        JSInterop.VerifyInvoke("micRecorder.connect", calledTimes: 2);
        JSInterop.VerifyInvoke("micRecorder.disconnect", calledTimes: 2);
        Assert.Equal(recording.MimeType, restarted.MimeType);
    }

    [Fact]
    public async Task ReleaseAsync_AfterRecordingDisposeFailure_DisposesTheCallbackReference()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true)
            .SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.Setup<string>("micRecorder.connect", _ => true).SetResult("audio/pcm;rate=16000");
        JSInterop.SetupVoid("micRecorder.disconnect", _ => true)
            .SetException(new JSException("disconnect failed"));
        JSInterop.SetupVoid("micRecorder.release", _ => true).SetVoidResult();
        var recorder = MicrophoneTestRecorder.Create(JSInterop.JSRuntime);
        var recording = await recorder.StartRecordingAsync(20, CancellationToken.None);

        await Assert.ThrowsAsync<JSException>(() => recording.DisposeAsync().AsTask());
        Assert.False(recording.IsDisposed);

        await recorder.ReleaseAsync(CancellationToken.None);

        Assert.True(recording.IsDisposed);
        JSInterop.VerifyInvoke("micRecorder.release", calledTimes: 1);
    }

    [Fact]
    public async Task StartAsync_WhenRetainedBrowserSessionIsDead_ReleasesAndReacquires()
    {
        var acquisition = JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true);
        acquisition.SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.Setup<string>("micRecorder.connect", _ => true).SetResult("audio/pcm;rate=16000");
        JSInterop.SetupVoid("micRecorder.disconnect", _ => true).SetVoidResult();
        JSInterop.Setup<bool>("micRecorder.isLive", _ => true).SetResult(false);
        JSInterop.SetupVoid("micRecorder.release", _ => true).SetVoidResult();
        var recorder = MicrophoneTestRecorder.Create(JSInterop.JSRuntime);
        await using var first = await recorder.StartAsync(CancellationToken.None);
        await recorder.StopAsync(CancellationToken.None);
        acquisition.SetResult(new MicrophoneAcquisitionDto { SessionId = "2" });

        await using var second = await recorder.StartAsync(CancellationToken.None);

        Assert.Equal(first.MimeType, second.MimeType);
        JSInterop.VerifyInvoke("micRecorder.isLive", calledTimes: 1);
        JSInterop.VerifyInvoke("micRecorder.release", calledTimes: 1);
        JSInterop.VerifyInvoke("micRecorder.acquire", calledTimes: 2);
    }

    [Fact]
    public async Task SetMicGateAsync_WhenDeadSessionReacquisitionWaitsForTheWriter_DoesNotDeadlockRelease()
    {
        var jsRuntime = new GateReleaseReacquisitionJsRuntime();
        var recorder = MicrophoneTestRecorder.Create(jsRuntime);
        await recorder.RequestAccessAsync(CancellationToken.None);

        var start = recorder.StartAsync(CancellationToken.None);
        await jsRuntime.WaitForOldSessionReleaseAsync();
        var gate = recorder.SetMicGateAsync(false, CancellationToken.None);
        await jsRuntime.WaitForGateWriteAsync();

        jsRuntime.FailGateWrite();
        await WaitForLifecycleVersionAsync(recorder, expectedMinimum: 1);
        jsRuntime.CompleteOldSessionRelease();
        await jsRuntime.WaitForSecondAcquisitionAsync();

        await Assert.ThrowsAsync<JSException>(() => gate.WaitAsync(TimeSpan.FromSeconds(5)));
        var startFailure = await Assert.ThrowsAsync<MicrophoneAcquisitionException>(
            () => start.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal(MicrophoneAcquisitionFailureCode.Unavailable, startFailure.Code);
        Assert.IsType<JSException>(startFailure.InnerException);

        Assert.Equal(2, jsRuntime.AcquireCallCount);
        Assert.Equal(["1", "2"], jsRuntime.ReleasedSessionIds);
    }

    private static async Task WaitForLifecycleVersionAsync(
        MicrophoneRecorder recorder,
        int expectedMinimum)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (recorder.SourceOwnershipGeneration < expectedMinimum)
        {
            await Task.Delay(1, timeout.Token);
        }
    }

    [Fact]
    public async Task BrowserSessionEnded_StopsTheRecordingAndTheNextStartReacquires()
    {
        var acquisition = JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true);
        acquisition.SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.SetupVoid("micRecorder.disconnect", _ => true).SetVoidResult();
        JSInterop.Setup<bool>("micRecorder.isLive", _ => true).SetResult(false);
        JSInterop.SetupVoid("micRecorder.release", _ => true).SetVoidResult();
        DotNetObjectReference<BrowserMicrophoneSession>? session = null;
        JSInterop.Setup<string>("micRecorder.connect", invocation =>
        {
            session = Assert.IsType<DotNetObjectReference<BrowserMicrophoneSession>>(invocation.Arguments[1]);
            return true;
        }).SetResult("audio/pcm;rate=16000");
        var recorder = MicrophoneTestRecorder.Create(JSInterop.JSRuntime);
        await recorder.StartAsync(CancellationToken.None);

        await session!.Value.OnCaptureFault("MICROPHONE_TRACK_ENDED");
        acquisition.SetResult(new MicrophoneAcquisitionDto { SessionId = "2" });
        await using var second = await recorder.StartAsync(CancellationToken.None);

        JSInterop.VerifyInvoke("micRecorder.disconnect", calledTimes: 1);
        JSInterop.VerifyInvoke("micRecorder.isLive", calledTimes: 1);
        JSInterop.VerifyInvoke("micRecorder.release", calledTimes: 1);
        JSInterop.VerifyInvoke("micRecorder.acquire", calledTimes: 2);
    }

    [Fact]
    public async Task StartAsync_WhenSupersedingStopFailsDuringConnect_RetriesBeforeReturningTheFailure()
    {
        var jsRuntime = new StoppedDuringConnectJsRuntime();
        var recorder = MicrophoneTestRecorder.Create(jsRuntime);

        var start = recorder.StartAsync(CancellationToken.None);
        await jsRuntime.WaitForConnectAsync();
        await Assert.ThrowsAsync<JSException>(() => jsRuntime.EndBrowserSessionAsync());
        jsRuntime.CompleteConnect();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => start.WaitAsync(TimeSpan.FromSeconds(5)));

        await using var next = await recorder.StartAsync(CancellationToken.None);

        Assert.Equal(2, jsRuntime.DisconnectCalls);
    }

    [Fact]
    public async Task StartAsync_WhenConnectAndReleaseFail_QuarantinesTheSessionUntilReleaseRetry()
    {
        var acquisition = JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true);
        acquisition.SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.Setup<string>("micRecorder.connect", _ => true).SetException(new JSException("connect failed"));
        var release = JSInterop.SetupVoid("micRecorder.release", _ => true);
        release.SetException(new JSException("release failed"));
        var recorder = MicrophoneTestRecorder.Create(JSInterop.JSRuntime);

        var failure = await Assert.ThrowsAsync<MicrophoneAcquisitionException>(
            () => recorder.StartAsync(CancellationToken.None));
        Assert.Equal(MicrophoneAcquisitionFailureCode.Unavailable, failure.Code);
        var cleanupFailure = Assert.IsType<AggregateException>(failure.InnerException);
        Assert.Equal(2, cleanupFailure.InnerExceptions.Count);
        var retryFailure = await Assert.ThrowsAsync<MicrophoneAcquisitionException>(
            () => recorder.RequestAccessAsync(CancellationToken.None));
        Assert.Equal(MicrophoneAcquisitionFailureCode.Unavailable, retryFailure.Code);
        Assert.IsType<JSException>(retryFailure.InnerException);
        release.SetVoidResult();
        acquisition.SetResult(new MicrophoneAcquisitionDto { SessionId = "2" });
        await recorder.RequestAccessAsync(CancellationToken.None);

        JSInterop.VerifyInvoke("micRecorder.release", calledTimes: 3);
        JSInterop.VerifyInvoke("micRecorder.acquire", calledTimes: 2);
    }

    [Fact]
    public async Task StartAsync_WhenBrowserConnectFails_PublishesUnavailableAfterCleanup()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true)
            .SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.Setup<string>("micRecorder.connect", _ => true)
            .SetException(new JSException("MIC_SESSION_NOT_LIVE"));
        JSInterop.SetupVoid("micRecorder.release", _ => true).SetVoidResult();
        var recorder = MicrophoneTestRecorder.Create(JSInterop.JSRuntime);

        var failure = await Assert.ThrowsAsync<MicrophoneAcquisitionException>(
            () => recorder.StartAsync(CancellationToken.None));

        Assert.Equal(MicrophoneAcquisitionFailureCode.Unavailable, failure.Code);
        Assert.IsType<JSException>(failure.InnerException);
        JSInterop.VerifyInvoke("micRecorder.release", calledTimes: 1);
    }

    [Fact]
    public async Task BrowserConnectCancellationWithoutCallerCancellation_ReachesWakeAsUnavailable()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true)
            .SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.Setup<string>("micRecorder.connect", _ => true)
            .SetException(new OperationCanceledException("browser connect canceled"));
        JSInterop.SetupVoid("micRecorder.release", _ => true).SetVoidResult();
        var captureClient = new RecorderBackedCaptureClient(MicrophoneTestRecorder.Create(JSInterop.JSRuntime));
        var phase = new WakeUtteranceCapturePhase(NoOpWakeWordTelemetrySink.Instance, "connect-cancellation");

        var outcome = await phase.CaptureAsync(
            new CaptureClientSegmentStreamSource(captureClient),
            CancellationToken.None);

        Assert.True(outcome.Failed);
        Assert.Equal(WakeCaptureUnavailableReason.Unavailable, outcome.UnavailableReason);
        JSInterop.VerifyInvoke("micRecorder.release", calledTimes: 1);
    }

    [Fact]
    public async Task BrowserGateFailureDuringAcquisition_ReachesWakeAsUnavailable()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true)
            .SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.SetupVoid("micRecorder.setMicGate", _ => true)
            .SetException(new JSException("gate failed"));
        JSInterop.SetupVoid("micRecorder.release", _ => true).SetVoidResult();
        var recorder = MicrophoneTestRecorder.Create(JSInterop.JSRuntime);
        await recorder.SetMicGateAsync(false, CancellationToken.None);
        var captureClient = new RecorderBackedCaptureClient(recorder);
        var phase = new WakeUtteranceCapturePhase(NoOpWakeWordTelemetrySink.Instance, "gate-failure");

        var outcome = await phase.CaptureAsync(
            new CaptureClientSegmentStreamSource(captureClient),
            CancellationToken.None);

        Assert.True(outcome.Failed);
        Assert.Equal(WakeCaptureUnavailableReason.Unavailable, outcome.UnavailableReason);
        JSInterop.VerifyInvoke("micRecorder.release", calledTimes: 1);
    }

    [Fact]
    public async Task BrowserAcquireFailure_ReachesWakeAsUnavailable()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true)
            .SetException(new JSException("acquire failed"));
        var captureClient = new RecorderBackedCaptureClient(MicrophoneTestRecorder.Create(JSInterop.JSRuntime));
        var phase = new WakeUtteranceCapturePhase(NoOpWakeWordTelemetrySink.Instance, "acquire-failure");

        var outcome = await phase.CaptureAsync(
            new CaptureClientSegmentStreamSource(captureClient),
            CancellationToken.None);

        Assert.True(outcome.Failed);
        Assert.Equal(WakeCaptureUnavailableReason.Unavailable, outcome.UnavailableReason);
    }

    [Fact]
    public async Task PartialBrowserAcquisitionFailure_ReachesWakeAsUnavailableAfterRelease()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true)
            .SetResult(new MicrophoneAcquisitionDto
            {
                SessionId = "1",
                ErrorName = "NotAllowedError",
            });
        JSInterop.SetupVoid("micRecorder.release", _ => true).SetVoidResult();
        var captureClient = new RecorderBackedCaptureClient(MicrophoneTestRecorder.Create(JSInterop.JSRuntime));
        var phase = new WakeUtteranceCapturePhase(NoOpWakeWordTelemetrySink.Instance, "partial-acquisition");

        var outcome = await phase.CaptureAsync(
            new CaptureClientSegmentStreamSource(captureClient),
            CancellationToken.None);

        Assert.True(outcome.Failed);
        Assert.Equal(WakeCaptureUnavailableReason.Unavailable, outcome.UnavailableReason);
        JSInterop.VerifyInvoke("micRecorder.release", calledTimes: 1);
    }

    [Fact]
    public async Task SetMicGateAsync_WhenTheTrackWriteFails_ReleasesTheUnhealthySession()
    {
        var acquisition = JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true);
        acquisition.SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        var gate = JSInterop.SetupVoid("micRecorder.setMicGate", _ => true);
        gate.SetException(new JSException("gate failed"));
        JSInterop.SetupVoid("micRecorder.release", _ => true).SetVoidResult();
        var recorder = MicrophoneTestRecorder.Create(JSInterop.JSRuntime);
        await recorder.RequestAccessAsync(CancellationToken.None);

        await Assert.ThrowsAsync<JSException>(() => recorder.SetMicGateAsync(false, CancellationToken.None));
        gate.SetVoidResult();
        acquisition.SetResult(new MicrophoneAcquisitionDto { SessionId = "2" });
        await recorder.RequestAccessAsync(CancellationToken.None);

        JSInterop.VerifyInvoke("micRecorder.release", calledTimes: 1);
        JSInterop.VerifyInvoke("micRecorder.acquire", calledTimes: 2);
    }

    [Fact]
    public async Task SetMicGateAsync_WhenGateAndReleaseFail_RetriesCleanupBeforeTheNextAcquisition()
    {
        var acquisition = JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true);
        acquisition.SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        var gate = JSInterop.SetupVoid("micRecorder.setMicGate", _ => true);
        gate.SetException(new JSException("gate failed"));
        var release = JSInterop.SetupVoid("micRecorder.release", _ => true);
        release.SetException(new JSException("release failed"));
        var recorder = MicrophoneTestRecorder.Create(JSInterop.JSRuntime);
        await recorder.RequestAccessAsync(CancellationToken.None);

        var failure = await Assert.ThrowsAsync<AggregateException>(
            () => recorder.SetMicGateAsync(true, CancellationToken.None));
        Assert.Collection(
            failure.InnerExceptions,
            gate => Assert.Equal("gate failed", gate.Message),
            cleanup => Assert.Equal("release failed", cleanup.Message));
        gate.SetVoidResult();
        release.SetVoidResult();
        acquisition.SetResult(new MicrophoneAcquisitionDto { SessionId = "2" });
        await recorder.RequestAccessAsync(CancellationToken.None);

        JSInterop.VerifyInvoke("micRecorder.release", calledTimes: 2);
        JSInterop.VerifyInvoke("micRecorder.acquire", calledTimes: 2);
    }

    [Fact]
    public async Task SetMicGateAsync_WhenAnOldFailureArrivesAfterReacquisition_PreservesTheNewSession()
    {
        var jsRuntime = new DelayedGateJsRuntime(firstGateFails: true);
        var recorder = MicrophoneTestRecorder.Create(jsRuntime);
        await recorder.RequestAccessAsync(CancellationToken.None);

        var staleGateWrite = recorder.SetMicGateAsync(false, CancellationToken.None);
        await jsRuntime.WaitForFirstGateWriteAsync();
        await recorder.ReleaseAsync(CancellationToken.None);
        var reacquisition = recorder.RequestAccessAsync(CancellationToken.None);
        var latestGateWrite = recorder.SetMicGateAsync(true, CancellationToken.None);
        Assert.Equal(1, jsRuntime.GateCallCount);

        jsRuntime.CompleteFirstGateWrite();
        await staleGateWrite;
        await Task.WhenAll(reacquisition, latestGateWrite);
        await recorder.RequestAccessAsync(CancellationToken.None);
        await recorder.ReleaseAsync(CancellationToken.None);

        Assert.Equal(2, jsRuntime.AcquireCallCount);
        Assert.Equal(["1", "2"], jsRuntime.ReleasedSessionIds);
        Assert.Equal([("1", false), ("2", true)], jsRuntime.MicGateCalls);
    }

    [Fact]
    public async Task SetMicGateAsync_SerializesWritesAndAppliesTheLatestStateAcrossReacquisition()
    {
        var jsRuntime = new DelayedGateJsRuntime(firstGateFails: false);
        var recorder = MicrophoneTestRecorder.Create(jsRuntime);
        await recorder.RequestAccessAsync(CancellationToken.None);

        var staleGateWrite = recorder.SetMicGateAsync(false, CancellationToken.None);
        await jsRuntime.WaitForFirstGateWriteAsync();
        await recorder.ReleaseAsync(CancellationToken.None);
        var reacquisition = recorder.RequestAccessAsync(CancellationToken.None);
        var latestGateWrite = recorder.SetMicGateAsync(true, CancellationToken.None);
        Assert.Equal(1, jsRuntime.GateCallCount);

        jsRuntime.CompleteFirstGateWrite();
        await Task.WhenAll(staleGateWrite, reacquisition, latestGateWrite);

        Assert.Equal(2, jsRuntime.AcquireCallCount);
        Assert.Equal(["1"], jsRuntime.ReleasedSessionIds);
        Assert.Equal([("1", false), ("2", true)], jsRuntime.MicGateCalls);
    }

    [Theory]
    [InlineData("NotFoundError", MicrophoneAcquisitionFailureCode.DeviceNotFound)]
    [InlineData("DevicesNotFoundError", MicrophoneAcquisitionFailureCode.DeviceNotFound)]
    [InlineData("OverconstrainedError", MicrophoneAcquisitionFailureCode.DeviceNotFound)]
    [InlineData("NotAllowedError", MicrophoneAcquisitionFailureCode.PermissionDenied)]
    [InlineData("PermissionDeniedError", MicrophoneAcquisitionFailureCode.PermissionDenied)]
    [InlineData("SecurityError", MicrophoneAcquisitionFailureCode.PermissionDenied)]
    public async Task RequestAccessAsync_MapsTheTypedBrowserErrorNameToAnApplicationCode(
        string browserErrorName,
        MicrophoneAcquisitionFailureCode expectedCode)
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true)
            .SetResult(new MicrophoneAcquisitionDto { ErrorName = browserErrorName });
        var recorder = MicrophoneTestRecorder.Create(JSInterop.JSRuntime);

        var exception = await Assert.ThrowsAsync<MicrophoneAcquisitionException>(
            () => recorder.RequestAccessAsync(CancellationToken.None));

        Assert.Equal(expectedCode, exception.Code);
    }

    public static TheoryData<string?, string?> MalformedAcquisitionResults => new()
    {
        { null, null },
        { "", null },
        { null, " " }
    };

    [Theory]
    [MemberData(nameof(MalformedAcquisitionResults))]
    public async Task RequestAccessAsync_RejectsMalformedAcquisitionResults(
        string? sessionId,
        string? errorName)
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true)
            .SetResult(new MicrophoneAcquisitionDto { SessionId = sessionId, ErrorName = errorName });
        var recorder = MicrophoneTestRecorder.Create(JSInterop.JSRuntime);

        var exception = await Assert.ThrowsAsync<MicrophoneAcquisitionException>(
            () => recorder.RequestAccessAsync(CancellationToken.None));

        Assert.Equal(MicrophoneAcquisitionFailureCode.Unavailable, exception.Code);
        Assert.Equal(
            "The microphone acquisition result is malformed.",
            Assert.IsType<InvalidOperationException>(exception.InnerException).Message);
    }

    [Fact]
    public async Task RequestAccessAsync_WhenMalformedResultContainsSession_ReleasesBeforePublishingFailure()
    {
        var acquisition = JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true);
        acquisition.SetResult(new MicrophoneAcquisitionDto { SessionId = "1", ErrorName = "NotAllowedError" });
        JSInterop.SetupVoid("micRecorder.release", invocation => invocation.Arguments[0] is string sessionId && sessionId == "1")
            .SetVoidResult();
        var recorder = MicrophoneTestRecorder.Create(JSInterop.JSRuntime);

        var exception = await Assert.ThrowsAsync<MicrophoneAcquisitionException>(
            () => recorder.RequestAccessAsync(CancellationToken.None));
        acquisition.SetResult(new MicrophoneAcquisitionDto { SessionId = "2" });
        await recorder.RequestAccessAsync(CancellationToken.None);

        Assert.Equal(MicrophoneAcquisitionFailureCode.Unavailable, exception.Code);
        Assert.Equal(
            "The microphone acquisition result is malformed.",
            Assert.IsType<InvalidOperationException>(exception.InnerException).Message);
        JSInterop.VerifyInvoke("micRecorder.release", calledTimes: 1);
        Assert.Equal(
            2,
            JSInterop.Invocations.Count(invocation => invocation.Identifier == "micRecorder.acquire"));
    }

    [Fact]
    public async Task RequestAccessAsync_WhenMalformedSessionReleaseFails_QuarantinesItUntilReleaseRetry()
    {
        var acquisition = JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true);
        acquisition.SetResult(new MicrophoneAcquisitionDto { SessionId = "1", ErrorName = " " });
        var release = JSInterop.SetupVoid("micRecorder.release", _ => true);
        release.SetException(new JSException("teardown failed"));
        var recorder = MicrophoneTestRecorder.Create(JSInterop.JSRuntime);

        var malformedFailure = await Assert.ThrowsAsync<MicrophoneAcquisitionException>(
            () => recorder.RequestAccessAsync(CancellationToken.None));
        Assert.Equal(MicrophoneAcquisitionFailureCode.Unavailable, malformedFailure.Code);
        Assert.IsType<AggregateException>(malformedFailure.InnerException);
        var retryFailure = await Assert.ThrowsAsync<MicrophoneAcquisitionException>(
            () => recorder.RequestAccessAsync(CancellationToken.None));
        Assert.Equal(MicrophoneAcquisitionFailureCode.Unavailable, retryFailure.Code);
        Assert.IsType<JSException>(retryFailure.InnerException);
        release.SetVoidResult();
        acquisition.SetResult(new MicrophoneAcquisitionDto { SessionId = "2" });
        await recorder.RequestAccessAsync(CancellationToken.None);

        JSInterop.VerifyInvoke("micRecorder.release", calledTimes: 3);
        Assert.Equal(
            2,
            JSInterop.Invocations.Count(invocation => invocation.Identifier == "micRecorder.acquire"));
    }

    [Fact]
    public async Task RequestAccessAsync_RejectsNullAcquisitionResult()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true).SetResult(null!);
        var recorder = MicrophoneTestRecorder.Create(JSInterop.JSRuntime);

        var exception = await Assert.ThrowsAsync<MicrophoneAcquisitionException>(
            () => recorder.RequestAccessAsync(CancellationToken.None));

        Assert.Equal(MicrophoneAcquisitionFailureCode.Unavailable, exception.Code);
        Assert.Equal(
            "The microphone acquisition result is malformed.",
            Assert.IsType<InvalidOperationException>(exception.InnerException).Message);
    }

    [Fact]
    public async Task RequestAccessAsync_AfterRelease_ReportsTheFullyReleasedInterval()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true).SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.SetupVoid("micRecorder.release", _ => true).SetVoidResult();
        var logger = new RecordingLogger<MicrophoneRecorder>();
        var recorder = MicrophoneTestRecorder.Create(JSInterop.JSRuntime, logger);

        await recorder.RequestAccessAsync(CancellationToken.None);
        await recorder.ReleaseAsync(CancellationToken.None);
        await recorder.RequestAccessAsync(CancellationToken.None);

        Assert.Contains(
            logger.Entries,
            entry => entry.Message.Contains("fully released", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RequestAccessAsync_AfterAcquisitionFailure_RetriesWithAFreshAcquisition()
    {
        var acquisition = JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true);
        acquisition.SetException(new JSException("NotReadableError: device busy"));
        var recorder = MicrophoneTestRecorder.Create(JSInterop.JSRuntime);

        var failure = await Assert.ThrowsAsync<MicrophoneAcquisitionException>(
            () => recorder.RequestAccessAsync(CancellationToken.None));
        Assert.Equal(MicrophoneAcquisitionFailureCode.Unavailable, failure.Code);
        Assert.IsType<JSException>(failure.InnerException);
        acquisition.SetResult(new MicrophoneAcquisitionDto { SessionId = "2" });

        await recorder.RequestAccessAsync(CancellationToken.None);

        Assert.Equal(
            2,
            JSInterop.Invocations.Count(invocation => invocation.Identifier == "micRecorder.acquire"));
    }

    [Fact]
    public void CaptureInfrastructure_UsesOneScopedRecorderForAllBrowserLifecyclePorts()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IJSRuntime>(JSInterop.JSRuntime);
        services.AddSpeechBrowserCaptureInfrastructure();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var recorder = scope.ServiceProvider.GetRequiredService<IMicrophoneRecorder>();
        var processing = scope.ServiceProvider.GetRequiredService<ICaptureTrackProcessingReader>();
        var micGate = scope.ServiceProvider.GetRequiredService<IMicGateBrowserBridge>();

        Assert.Same(recorder, processing);
        Assert.Same(recorder, micGate);
    }

    [Fact]
    public void CaptureInfrastructure_SecondCallRegistersASecondRecorder()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IJSRuntime>(JSInterop.JSRuntime);
        services.AddSpeechBrowserCaptureInfrastructure();
        services.AddSpeechBrowserCaptureInfrastructure();

        Assert.Equal(2, services.Count(descriptor => descriptor.ServiceType == typeof(MicrophoneRecorder)));
    }

}
