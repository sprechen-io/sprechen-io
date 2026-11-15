using System.Diagnostics;
using Sprechen.Service.Grpc;
using Xunit;

namespace Sprechen.Service.Tests.Grpc;

public sealed class PcmAudioStreamPacerTests
{
    [Fact]
    public async Task PaceAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        var pacer = new PcmAudioStreamPacer();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            pacer.PaceAsync(bytesSent: 1, sampleRateHz: 24_000, cts.Token).AsTask());
    }

    [Fact]
    public async Task PaceAsync_PcmBytes_DelaysAtLeastAudioDuration()
    {
        var pacer = new PcmAudioStreamPacer();
        const int sampleRateHz = 24_000;
        var audioDuration = TimeSpan.FromMilliseconds(200);
        var bytes = (int)(sampleRateHz * 2 * audioDuration.TotalSeconds);

        var sw = Stopwatch.StartNew();
        await pacer.PaceAsync(bytes, sampleRateHz, CancellationToken.None);
        sw.Stop();

        Assert.True(
            sw.Elapsed >= TimeSpan.FromMilliseconds(150),
            $"Expected pacing delay >=150ms, got {sw.Elapsed.TotalMilliseconds:0}ms.");
    }
}

