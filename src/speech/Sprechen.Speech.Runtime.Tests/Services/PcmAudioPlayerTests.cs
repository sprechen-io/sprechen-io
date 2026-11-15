using Bunit;
using Microsoft.JSInterop;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class PcmAudioPlayerTests : TestContext
{
    private static readonly PcmAudioPlayerInvocationResult SuccessfulInvocation =
        new(true, ErrorCode: null, DiagnosticDetail: null);

    [Fact]
    public async Task InitializeAsync_WhenSampleRateIsNonPositive_Throws()
    {
        var player = new PcmAudioPlayer(JSInterop.JSRuntime, new AssistantPlaybackOutputLatencyState());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => player.InitializeAsync(0, CancellationToken.None));
    }

    [Fact]
    public async Task DrainAsync_WhenNotInitialized_DoesNothing()
    {
        var player = new PcmAudioPlayer(JSInterop.JSRuntime, new AssistantPlaybackOutputLatencyState());

        await player.DrainAsync(CancellationToken.None);
    }

    [Fact]
    public async Task EnqueuePcm16LeAsync_WhenNotInitialized_Throws()
    {
        var player = new PcmAudioPlayer(JSInterop.JSRuntime, new AssistantPlaybackOutputLatencyState());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            player.EnqueuePcm16LeAsync(new byte[] { 1, 2 }, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task EnqueuePcm16LeAsync_WhenEmpty_DoesNothing()
    {
        JSInterop
            .Setup<PcmAudioPlayerInvocationResult>("pcmPlayer.init", 24000)
            .SetResult(SuccessfulInvocation);

        var player = new PcmAudioPlayer(JSInterop.JSRuntime, new AssistantPlaybackOutputLatencyState());
        await player.InitializeAsync(24000, CancellationToken.None);

        await player.EnqueuePcm16LeAsync(Array.Empty<byte>(), CancellationToken.None);
    }

    [Fact]
    public async Task InitializeAsync_WhenInitialized_AllowsResetDrainAndEnqueue()
    {
        JSInterop
            .Setup<PcmAudioPlayerInvocationResult>("pcmPlayer.init", 24000)
            .SetResult(SuccessfulInvocation);
        JSInterop
            .Setup<PcmAudioPlayerInvocationResult>("pcmPlayer.reset")
            .SetResult(SuccessfulInvocation);
        JSInterop
            .Setup<PcmAudioPlayerInvocationResult>("pcmPlayer.drain")
            .SetResult(SuccessfulInvocation);
        JSInterop
            .Setup<PcmAudioPlayerInvocationResult>("pcmPlayer.enqueue", _ => true)
            .SetResult(SuccessfulInvocation);
        JSInterop
            .Setup<PcmAudioPlayerInvocationResult>("pcmPlayer.setMakeup", 1.25d)
            .SetResult(SuccessfulInvocation);

        var player = new PcmAudioPlayer(JSInterop.JSRuntime, new AssistantPlaybackOutputLatencyState());
        await player.InitializeAsync(24000, CancellationToken.None);

        await player.EnqueuePcm16LeAsync(new byte[] { 1, 2 }, CancellationToken.None);
        await player.SetMakeupAsync(1.25d, CancellationToken.None);
        await player.ResetAsync(CancellationToken.None);
        await player.DrainAsync(CancellationToken.None);

        JSInterop.VerifyInvoke("pcmPlayer.init");
        JSInterop.VerifyInvoke("pcmPlayer.enqueue");
        JSInterop.VerifyInvoke("pcmPlayer.setMakeup");
        JSInterop.VerifyInvoke("pcmPlayer.reset");
        JSInterop.VerifyInvoke("pcmPlayer.drain");
    }

    [Fact]
    public async Task WarmupAsync_WhenBrowserReportsPlaybackTerminal_ThrowsTypedProviderFailure()
    {
        JSInterop
            .Setup<PcmAudioPlayerInvocationResult>("pcmPlayer.warmup")
            .SetResult(new PcmAudioPlayerInvocationResult(
                Succeeded: false,
                ErrorCode: "PCM_PLAYER_KEEP_ALIVE_FAILED",
                DiagnosticDetail: "Non-zero output floor could not start."));

        var player = new PcmAudioPlayer(JSInterop.JSRuntime, new AssistantPlaybackOutputLatencyState());

        var exception = await Assert.ThrowsAsync<SpeechErrorException>(
            () => player.WarmupAsync(CancellationToken.None).AsTask());

        Assert.Equal(SpeechErrorCode.ProviderFailure, exception.ErrorCode);
        Assert.Contains("PCM_PLAYER_KEEP_ALIVE_FAILED", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WarmupAsync_WhenInteropRejects_ThrowsTypedProviderFailureWithoutMessageClassification()
    {
        JSInterop
            .Setup<PcmAudioPlayerInvocationResult>("pcmPlayer.warmup")
            .SetException(new JSException("arbitrary bridge rejection"));

        var player = new PcmAudioPlayer(JSInterop.JSRuntime, new AssistantPlaybackOutputLatencyState());

        var exception = await Assert.ThrowsAsync<SpeechErrorException>(
            () => player.WarmupAsync(CancellationToken.None).AsTask());

        Assert.Equal(SpeechErrorCode.ProviderFailure, exception.ErrorCode);
        Assert.IsType<JSException>(exception.InnerException);
    }

    [Fact]
    public async Task WarmupAsync_WhenInteropTimesOutWithoutCallerCancellation_ThrowsTypedProviderFailure()
    {
        JSInterop
            .Setup<PcmAudioPlayerInvocationResult>("pcmPlayer.warmup")
            .SetException(new TaskCanceledException("default JS interop timeout"));

        var player = new PcmAudioPlayer(JSInterop.JSRuntime, new AssistantPlaybackOutputLatencyState());

        var exception = await Assert.ThrowsAsync<SpeechErrorException>(
            () => player.WarmupAsync(CancellationToken.None).AsTask());

        Assert.Equal(SpeechErrorCode.ProviderFailure, exception.ErrorCode);
        Assert.IsType<TaskCanceledException>(exception.InnerException);
    }

    [Fact]
    public async Task WarmupAsync_WhenOutputLatencyReadTimesOutWithoutCallerCancellation_RemainsSuccessful()
    {
        JSInterop
            .Setup<PcmAudioPlayerInvocationResult>("pcmPlayer.warmup")
            .SetResult(SuccessfulInvocation);
        JSInterop
            .Setup<double>("pcmPlayer.getOutputLatencyMs")
            .SetException(new TaskCanceledException("best-effort latency read timed out"));

        var player = new PcmAudioPlayer(JSInterop.JSRuntime, new AssistantPlaybackOutputLatencyState());

        await player.WarmupAsync(CancellationToken.None);

        JSInterop.VerifyInvoke("pcmPlayer.warmup");
        JSInterop.VerifyInvoke("pcmPlayer.getOutputLatencyMs");
    }
}
