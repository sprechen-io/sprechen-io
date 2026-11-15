using Google.Protobuf;
using Sprechen.Proto;
using Sprechen.Web.HostSupport.Grpc.Proxy;
using Xunit;

namespace Sprechen.Web.HostSupport.Tests.Grpc;

public sealed class SpeechProxyAudioStreamAccumulatorTests
{
    [Fact]
    public void TryAdd_SkipsEmptyAudioAndTracksFirstPositiveSampleRate()
    {
        var accumulator = new SpeechProxyAudioStreamAccumulator();

        Assert.False(accumulator.TryAdd(new AudioResponse()));
        Assert.True(accumulator.TryAdd(new AudioResponse
        {
            Data = ByteString.CopyFrom([0x01]),
            SampleRateHz = 0
        }));
        Assert.True(accumulator.TryAdd(new AudioResponse
        {
            Data = ByteString.CopyFrom([0x02, 0x03]),
            SampleRateHz = 24_000
        }));

        var summary = accumulator.ToSummary();

        Assert.Equal(2, summary.ChunkCount);
        Assert.Equal(3, summary.ByteCount);
        Assert.Null(summary.FirstSampleRateHz);
    }
}
