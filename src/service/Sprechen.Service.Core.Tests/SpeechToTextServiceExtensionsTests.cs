using Moq;
using Xunit;

namespace Sprechen.Service.Core.Tests;

public sealed class SpeechToTextServiceExtensionsTests
{
    [Fact]
    public async Task TranscribeStreamingAsync_PassesNullLanguageCode()
    {
        var chunks = Chunks(new byte[] { 1, 2 });
        var service = new Mock<ISpeechToTextService>(MockBehavior.Strict);
        service.Setup(s => s.TranscribeStreamingAsync(chunks, AudioMimeType.Pcm, CancellationToken.None, null))
            .ReturnsAsync(new SpeechTranscript("ok", 90));

        var result = await service.Object.TranscribeStreamingAsync(chunks, AudioMimeType.Pcm, CancellationToken.None);

        Assert.Equal("ok", result.Text);
        Assert.Equal(90, result.ConfidencePercent);
        service.VerifyAll();
    }

    [Fact]
    public async Task TranscribeStreamingFeed_PassesNullLanguageCode()
    {
        var chunks = Chunks(new byte[] { 1, 2 });
        var service = new Mock<ISpeechToTextService>(MockBehavior.Strict);
        service.Setup(s => s.TranscribeStreamingFeed(chunks, AudioMimeType.Pcm, CancellationToken.None, null))
            .Returns(Transcripts("one", "two"));

        var results = new List<SpeechTranscript>();
        await foreach (var transcript in service.Object.TranscribeStreamingFeed(chunks, AudioMimeType.Pcm, CancellationToken.None))
        {
            results.Add(transcript);
        }

        Assert.Collection(
            results,
            first => Assert.Equal("one", first.Text),
            second => Assert.Equal("two", second.Text));
        service.VerifyAll();
    }

    [Fact]
    public async Task TranscribeStreamingAsync_WhenServiceIsNull_Throws()
    {
        ISpeechToTextService? service = null;

        await Assert.ThrowsAsync<ArgumentNullException>(() => service!.TranscribeStreamingAsync(
            Chunks(new byte[] { 1 }),
            AudioMimeType.Pcm,
            CancellationToken.None));
    }

    [Fact]
    public void TranscribeStreamingFeed_WhenServiceIsNull_Throws()
    {
        ISpeechToTextService? service = null;

        Assert.Throws<ArgumentNullException>(() => service!.TranscribeStreamingFeed(
            Chunks(new byte[] { 1 }),
            AudioMimeType.Pcm,
            CancellationToken.None));
    }

    private static async IAsyncEnumerable<byte[]> Chunks(params byte[][] chunks)
    {
        foreach (var chunk in chunks)
        {
            await Task.Yield();
            yield return chunk;
        }
    }

    private static async IAsyncEnumerable<SpeechTranscript> Transcripts(params string[] transcripts)
    {
        foreach (var transcript in transcripts)
        {
            await Task.Yield();
            yield return new SpeechTranscript(transcript, null);
        }
    }
}
