using System.Text;
using Sprechen.Service.Deepgram.Services;
using Xunit;

namespace Sprechen.Service.Deepgram.Tests.Services;

public sealed class DeepgramAudioPayloadTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(16_000, 500)]
    [InlineData(32_000, 1_000)]
    [InlineData(48_000, 1_500)]
    public void GetPcmDurationMs_UsesSixteenKilohertzMonoSixteenBitPcm(int byteCount, int expectedDurationMs)
    {
        var durationMs = DeepgramAudioPayload.GetPcmDurationMs(byteCount);

        Assert.Equal(expectedDurationMs, durationMs);
    }

    [Fact]
    public void Prepare_Pcm_WrapsPayloadInSixteenKilohertzWavHeader()
    {
        var pcm = new byte[] { 1, 2, 3, 4 };

        var payload = DeepgramAudioPayload.Prepare(pcm, AudioMimeType.Pcm);

        Assert.Equal(44 + pcm.Length, payload.Length);
        Assert.Equal("RIFF", Encoding.ASCII.GetString(payload, 0, 4));
        Assert.Equal("WAVE", Encoding.ASCII.GetString(payload, 8, 4));
        Assert.Equal("fmt ", Encoding.ASCII.GetString(payload, 12, 4));
        Assert.Equal(1, BitConverter.ToInt16(payload, 20));
        Assert.Equal(1, BitConverter.ToInt16(payload, 22));
        Assert.Equal(16_000, BitConverter.ToInt32(payload, 24));
        Assert.Equal(32_000, BitConverter.ToInt32(payload, 28));
        Assert.Equal(2, BitConverter.ToInt16(payload, 32));
        Assert.Equal(16, BitConverter.ToInt16(payload, 34));
        Assert.Equal("data", Encoding.ASCII.GetString(payload, 36, 4));
        Assert.Equal(pcm.Length, BitConverter.ToInt32(payload, 40));
        Assert.Equal(pcm, payload.Skip(44).ToArray());
    }

    [Theory]
    [InlineData(AudioMimeType.Wav)]
    [InlineData(AudioMimeType.Mp3)]
    [InlineData(AudioMimeType.Flac)]
    [InlineData(AudioMimeType.OggOpus)]
    [InlineData(AudioMimeType.WebmOpus)]
    public void Prepare_AlreadyEncodedMimeTypes_ReturnsOriginalPayload(AudioMimeType mime)
    {
        var audio = new byte[] { 9, 8, 7 };

        var payload = DeepgramAudioPayload.Prepare(audio, mime);

        Assert.Same(audio, payload);
    }

    [Fact]
    public void Prepare_UnspecifiedMimeType_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            DeepgramAudioPayload.Prepare(new byte[] { 1 }, AudioMimeType.Unspecified));

        Assert.Contains("explicit audio mime type", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReadAllAsync_ConcatenatesChunks()
    {
        var payload = await DeepgramAudioPayload.ReadAllAsync(ReadFramesAsync(), CancellationToken.None);

        Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, payload);
    }

    private static async IAsyncEnumerable<byte[]> ReadFramesAsync()
    {
        await Task.Yield();
        yield return new byte[] { 1, 2 };
        yield return Array.Empty<byte>();
        yield return new byte[] { 3, 4, 5 };
    }
}
