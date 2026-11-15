using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class PcmAudioPlaybackLifecycleTests
{
    [Fact]
    public async Task EnsureInitializedAsync_WhenSampleRateIsUnchanged_InitializesOnlyOnce()
    {
        var pcmPlayer = new Mock<IPcmAudioPlayer>(MockBehavior.Strict);
        pcmPlayer
            .Setup(player => player.InitializeAsync(PcmAudioDefaults.DEFAULT_SAMPLE_RATE_HZ, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var lifecycle = new PcmAudioPlaybackLifecycle(
            pcmPlayer.Object,
            NullLogger<PcmAudioPlaybackLifecycle>.Instance);

        await lifecycle.EnsureInitializedAsync(PcmAudioDefaults.DEFAULT_SAMPLE_RATE_HZ, CancellationToken.None);
        await lifecycle.EnsureInitializedAsync(PcmAudioDefaults.DEFAULT_SAMPLE_RATE_HZ, CancellationToken.None);

        pcmPlayer.Verify(
            player => player.InitializeAsync(PcmAudioDefaults.DEFAULT_SAMPLE_RATE_HZ, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ResetBestEffortAsync_WhenInitializedResetFails_DoesNotThrow()
    {
        var pcmPlayer = new Mock<IPcmAudioPlayer>(MockBehavior.Strict);
        pcmPlayer
            .Setup(player => player.InitializeAsync(PcmAudioDefaults.DEFAULT_SAMPLE_RATE_HZ, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        pcmPlayer
            .Setup(player => player.ResetAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("reset failed"));

        var lifecycle = new PcmAudioPlaybackLifecycle(
            pcmPlayer.Object,
            NullLogger<PcmAudioPlaybackLifecycle>.Instance);

        await lifecycle.EnsureInitializedAsync(PcmAudioDefaults.DEFAULT_SAMPLE_RATE_HZ, CancellationToken.None);

        await lifecycle.ResetBestEffortAsync();

        pcmPlayer.Verify(player => player.ResetAsync(CancellationToken.None), Times.Once);
    }
}
