using Google.Protobuf;
using Sprechen.Proto;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using Xunit;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class GrpcPcmAudioStreamReaderTests
{
    [Fact]
    public async Task ReadAlignedPcm16LeAsync_OddBytesAcrossMessages_MergesCarryAndKeepsSampleRate()
    {
        var responses = new[]
        {
            new AudioResponse
            {
                MimeType = AudioMimeType.AudioPcm,
                SampleRateHz = 16_000,
                Data = ByteString.CopyFrom(new byte[] { 0x1, 0x2, 0x3 })
            },
            new AudioResponse
            {
                MimeType = AudioMimeType.AudioPcm,
                SampleRateHz = 0,
                Data = ByteString.CopyFrom(new byte[] { 0x4, 0x5 })
            }
        };

        var stream = new SequenceAsyncStreamReader<AudioResponse>(responses);
        var reader = new GrpcPcmAudioStreamReader();

        var result = new List<(byte[] bytes, int sampleRateHz)>();
        await foreach (var item in reader.ReadAlignedPcm16LeAsync(stream, CancellationToken.None))
        {
            result.Add(item);
        }

        Assert.Equal(2, result.Count);
        Assert.Equal(16_000, result[0].sampleRateHz);
        Assert.Equal(16_000, result[1].sampleRateHz);
        Assert.Equal(new byte[] { 0x1, 0x2 }, result[0].bytes);
        Assert.Equal(new byte[] { 0x3, 0x4 }, result[1].bytes);
    }

    [Fact]
    public async Task ReadAlignedPcm16LeAsync_NonPcm_Throws()
    {
        var responses = new[]
        {
            new AudioResponse
            {
                MimeType = AudioMimeType.AudioMp3,
                Data = ByteString.CopyFrom(new byte[] { 0x1, 0x2 })
            }
        };

        var stream = new SequenceAsyncStreamReader<AudioResponse>(responses);
        var reader = new GrpcPcmAudioStreamReader();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in reader.ReadAlignedPcm16LeAsync(stream, CancellationToken.None))
            {
            }
        });

        Assert.Contains("Expected PCM audio", ex.Message, StringComparison.Ordinal);
    }
}


