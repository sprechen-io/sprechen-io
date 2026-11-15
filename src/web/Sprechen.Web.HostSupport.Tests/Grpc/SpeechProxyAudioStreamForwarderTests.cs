using Google.Protobuf;
using Grpc.Core;
using Sprechen.Web.HostSupport.Grpc.Adapter;
using Sprechen.Web.HostSupport.Grpc.Proxy;
using SprechenProto = Sprechen.Proto;
using Xunit;

namespace Sprechen.Web.HostSupport.Tests.Grpc;

public sealed class SpeechProxyAudioStreamForwarderTests
{
    [Fact]
    public async Task StreamAsync_SkipsEmptyChunksAndReturnsForwardingSummary()
    {
        var audioChunk = new SprechenProto.AudioResponse
        {
            MimeType = SprechenProto.AudioMimeType.AudioPcm,
            SampleRateHz = 24_000,
            Data = ByteString.CopyFrom([0x1, 0x2, 0x3])
        };
        using var call = new AsyncServerStreamingCall<SprechenProto.AudioResponse>(
            new SequenceAsyncStreamReader<SprechenProto.AudioResponse>(
            [
                new SprechenProto.AudioResponse(),
                audioChunk
            ]),
            Task.FromResult(new Metadata()),
            static () => new Status(StatusCode.OK, string.Empty),
            static () => new Metadata(),
            static () => { });
        var writer = new RecordingServerStreamWriter<SprechenProto.AudioResponse>();

        var summary = await new SpeechProxyAudioStreamForwarder().StreamAsync(call, writer, new SpeechProxyAudioStreamAccumulator(), CancellationToken.None);

        var forwarded = Assert.Single(writer.Items);
        Assert.Equal(audioChunk.Data.ToByteArray(), forwarded.Data.ToByteArray());
        Assert.Equal(1, summary.ChunkCount);
        Assert.Equal(3, summary.ByteCount);
        Assert.Equal(24_000, summary.FirstSampleRateHz);
    }
}
